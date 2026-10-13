using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ShopCatalogSO의 품목으로 상점을 운영하는 데이터 소스. (ShopScene 단독 테스트용)
/// - 같은 품목은 동시에 진열하지 않는다. 산 품목도 리롤하면 다시 나올 수 있다.
/// - 산 칸은 비워 두고, 리롤하면 빈 칸까지 모두 새로 채운다.
/// - 가격은 진열될 때마다 기본 가격 ±변동 비율 안에서 정해진다.
/// - 리롤 비용은 상점을 열 때 기본 비용으로 시작해, 리롤할 때마다 일정 비율씩 오른다.
/// 골드는 원정 중이면 원정 지갑(GameSession → PlayerHoldings → ExpeditionWallet)을 쓴다.
/// 원정 중이 아니면(ShopScene 단독 테스트) 인스펙터의 시작 골드를 따로 관리한다.
///
/// 구매한 품목 지급
/// - 룬 워드 : 완성품 1개 (OwnedRuneWords). 룬북에서 룬 문자 없이 1번 장착할 수 있다.
/// - 룬 문자 : 무작위 룬 문자 RuneStoneAmount개 (PlayerRuneManager의 룬 문자 인벤토리)
/// - 포션 : 즉시 최대 체력 × HealRatio만큼 회복 (PlayerHealth)
/// 지급할 수 없는 상태면(플레이어나 인벤토리가 없음) 골드를 쓰지 않고 구매를 막는다.
///
/// 단독 테스트 (_openOnStart가 켜져 있고 원정 지갑이 없을 때)
/// - 시작하면 ShopPresenter.OpenShop으로 열어 입장 연출과 효과음까지 실제처럼 나온다.
/// - 플레이어가 없어도 룬 문자와 포션을 살 수 있다. (룬 문자는 로그만 남기고, 포션은 테스트 체력을 회복한다)
/// - 상점을 나간 뒤 _testReopenKey를 누르면 다시 연다.
/// </summary>
public class CatalogShopDataSource : ShopDataSource
{
    [SerializeField] private ShopCatalogSO _catalog;
    [Tooltip("원정 지갑이 없을 때(단독 테스트) 쓰는 시작 골드")]
    [SerializeField, Min(0)] private int _startingGold = 50000;
    [SerializeField] private bool _openOnStart = true;
    [Tooltip("단독 테스트에서 상점을 나간 뒤 다시 여는 키")]
    [SerializeField] private KeyCode _testReopenKey = KeyCode.F5;

    [Header("테스트용 플레이어 체력 (씬에 PlayerHealth가 없을 때 사용)")]
    [SerializeField, Min(0f)] private float _testCurrentHealth = 45f;
    [SerializeField, Min(1f)] private float _testMaxHealth = 100f;

    private readonly List<ShopOfferData> _offers = new();
    private readonly List<ShopCatalogSO.Item> _pickBuffer = new();
    private int _gold;
    private int _rerollCount;

    public override IReadOnlyList<ShopOfferData> Offers => _offers;

    private Wallet _boundWallet;
    private ShopPresenter _testPresenter;

    /// <summary>ShopScene 단독 테스트 중인지. (원정 중이 아님)</summary>
    private bool IsStandaloneTest => _openOnStart && CurrentWallet == null;

    public override int Gold => CurrentWallet != null ? CurrentWallet.Balance : _gold;

    /// <summary>원정 중인 지갑. 없으면 null (테스트용 골드 사용)</summary>
    private static Wallet CurrentWallet
    {
        get
        {
            GameSession session = GameSession.Instance;
            return session != null && session.IsReady ? session.Current?.Holdings?.ExpeditionWallet : null;
        }
    }

    public override int RerollCost
    {
        get
        {
            if (_catalog == null)
            {
                return 0;
            }

            float cost = _catalog.RerollBaseCost * Mathf.Pow(1f + _catalog.RerollCostGrowth, _rerollCount);
            return RoundPrice(cost);
        }
    }

    private void Awake()
    {
        _gold = _startingGold;
        if (_catalog == null)
        {
            Debug.LogError("[CatalogShopDataSource] ShopCatalogSO가 할당되지 않았습니다.", this);
        }
    }

    private void Start()
    {
        if (!_openOnStart)
        {
            return;
        }

        // 실제 게임처럼 상점 창을 열어 입장 연출(인사, 효과음)까지 보이게 한다.
        _testPresenter = FindAnyObjectByType<ShopPresenter>(FindObjectsInactive.Include);
        if (_testPresenter != null)
        {
            _testPresenter.OpenShop();
        }
        else
        {
            Open();
        }
    }

    private void Update()
    {
        if (!_openOnStart || _testPresenter == null || _testPresenter.isActiveAndEnabled ||
            !Input.GetKeyDown(_testReopenKey))
        {
            return;
        }

        _testPresenter.OpenShop();
    }

    public override bool TryGetPlayerHealth(out float current, out float max)
    {
        if (base.TryGetPlayerHealth(out current, out max))
        {
            return true;
        }

        max = _testMaxHealth;
        current = Mathf.Min(_testCurrentHealth, _testMaxHealth);
        return true;
    }

    public override void Open()
    {
        BindWallet();
        _rerollCount = 0;
        FillOffers();
        RaiseChanged();
    }

    public override bool TryBuy(int slotIndex)
    {
        if (!CanBuy(slotIndex))
        {
            return false;
        }

        ShopOfferData offer = _offers[slotIndex];
        ShopCatalogSO.Item item = FindItem(offer.Id);
        if (!CanGrant(offer, item))
        {
            Debug.LogWarning($"[Shop] {offer.DisplayName}을(를) 지급할 수 없어 구매를 취소했습니다.", this);
            return false;
        }

        if (!TrySpend(offer.Price))
        {
            return false;
        }

        _offers[slotIndex] = null;
        GrantItem(offer, item);

        Debug.Log($"[Shop] Bought {offer.DisplayName} ({offer.Type}) for {offer.Price}. Gold left: {Gold}", this);
        RaisePurchased(offer);
        RaiseChanged();
        return true;
    }

    public override bool TryReroll()
    {
        if (!CanReroll())
        {
            return false;
        }

        int cost = RerollCost;
        if (!TrySpend(cost))
        {
            return false;
        }

        _rerollCount++;
        FillOffers();

        Debug.Log($"[Shop] Rerolled for {cost}. Next reroll: {RerollCost}. Gold left: {Gold}", this);
        RaiseChanged();
        return true;
    }

    private bool TrySpend(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        Wallet wallet = CurrentWallet;
        if (wallet != null)
        {
            return wallet.TrySpend(amount);
        }

        if (_gold < amount)
        {
            return false;
        }

        _gold -= amount;
        return true;
    }

    /// <summary>원정 지갑의 잔액이 바뀌면(몬스터 처치 등) 상점 표시도 갱신한다.</summary>
    private void BindWallet()
    {
        Wallet wallet = CurrentWallet;
        if (_boundWallet == wallet)
        {
            return;
        }

        if (_boundWallet != null)
        {
            _boundWallet.BalanceChanged -= HandleBalanceChanged;
        }

        _boundWallet = wallet;
        if (_boundWallet != null)
        {
            _boundWallet.BalanceChanged += HandleBalanceChanged;
        }
    }

    private void HandleBalanceChanged(int balance)
    {
        RaiseChanged();
    }

    private void OnDestroy()
    {
        if (_boundWallet != null)
        {
            _boundWallet.BalanceChanged -= HandleBalanceChanged;
            _boundWallet = null;
        }
    }

    /// <summary>지금 이 품목을 지급할 수 있는지. 지급할 수 없으면 골드를 쓰지 않는다.</summary>
    /// <summary>체력이 가득 찼으면 포션은 살 수 없다.</summary>
    public override bool CanPurchase(ShopOfferData offer)
    {
        if (offer == null)
        {
            return false;
        }

        if (offer.Type != ShopItemType.Potion)
        {
            return true;
        }

        return !TryGetPlayerHealth(out float current, out float max) || current < max;
    }

    private bool CanGrant(ShopOfferData offer, ShopCatalogSO.Item item)
    {
        switch (offer.Type)
        {
            case ShopItemType.RuneWord:
                return item != null && !string.IsNullOrEmpty(item.RuneEffectId);

            case ShopItemType.RuneStone:
                return CanGrantRuneStones() || IsStandaloneTest;

            case ShopItemType.Potion:
                PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
                if (health == null && IsStandaloneTest)
                {
                    return offer.HealRatio > 0f && _testCurrentHealth < _testMaxHealth;
                }

                return health != null && !health.IsDead && offer.HealRatio > 0f &&
                       health.CurrentHealth < health.MaxHealth;

            default:
                // 연구 물질 등 폐기된 품목
                return false;
        }
    }

    /// <summary>산 품목을 지급한다.</summary>
    private void GrantItem(ShopOfferData offer, ShopCatalogSO.Item item)
    {
        switch (offer.Type)
        {
            case ShopItemType.RuneWord:
                OwnedRuneWords.Add(item.RuneEffectId);
                Debug.Log($"[Shop] 룬 워드 완성품 지급: {item.RuneEffectId}", this);
                break;

            case ShopItemType.RuneStone:
                if (!CanGrantRuneStones())
                {
                    Debug.Log($"[Shop] (단독 테스트) 룬 문자 {(item != null ? item.RuneStoneAmount : 1)}개 지급 생략", this);
                    break;
                }

                GrantRandomRuneStones(item != null ? item.RuneStoneAmount : 1);
                break;

            case ShopItemType.Potion:
                PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
                if (health == null)
                {
                    _testCurrentHealth = Mathf.Min(_testMaxHealth, _testCurrentHealth + _testMaxHealth * offer.HealRatio);
                    Debug.Log($"[Shop] (단독 테스트) 체력 회복 → {_testCurrentHealth:0}/{_testMaxHealth:0}", this);
                    break;
                }

                float healAmount = health.MaxHealth * offer.HealRatio;
                health.Heal(healAmount);
                Debug.Log($"[Shop] 체력 {healAmount:0} 회복 → {health.CurrentHealth:0}/{health.MaxHealth:0}", this);
                break;
        }
    }

    private static bool CanGrantRuneStones()
    {
        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        RuneStoneDatabase stoneDatabase = RuneStoneDatabase.Instance;
        return runeManager != null && runeManager.IsInitialized &&
               stoneDatabase != null && stoneDatabase.DatabaseReady && stoneDatabase.GetAll().Count > 0;
    }

    /// <summary>룬 문자를 amount개 지급한다. 하나마다 종류를 무작위로 고른다.</summary>
    private void GrantRandomRuneStones(int amount)
    {
        IReadOnlyList<RuneStoneDefinition> definitions = RuneStoneDatabase.Instance.GetAll();
        RuneStoneInventory inventory = PlayerRuneManager.Instance.RuneStoneInventory;

        for (int i = 0; i < Mathf.Max(1, amount); i++)
        {
            RuneStoneDefinition definition = definitions[Random.Range(0, definitions.Count)];
            inventory.TryAddRuneStone(definition.RuneCharacter, 1);
        }

        Debug.Log($"[Shop] 무작위 룬 문자 {Mathf.Max(1, amount)}개 지급", this);
    }

    private ShopCatalogSO.Item FindItem(string id)
    {
        if (_catalog == null)
        {
            return null;
        }

        foreach (ShopCatalogSO.Item item in _catalog.Items)
        {
            if (item != null && item.Id == id)
            {
                return item;
            }
        }

        return null;
    }

    private void FillOffers()
    {
        _offers.Clear();
        if (_catalog == null)
        {
            return;
        }

        // 같은 품목이 동시에 나오지 않도록 목록을 섞은 뒤 앞에서부터 고른다.
        _pickBuffer.Clear();
        foreach (ShopCatalogSO.Item item in _catalog.Items)
        {
            if (item != null)
            {
                _pickBuffer.Add(item);
            }
        }

        for (int i = _pickBuffer.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_pickBuffer[i], _pickBuffer[j]) = (_pickBuffer[j], _pickBuffer[i]);
        }

        for (int i = 0; i < _catalog.SlotCount; i++)
        {
            // 품목 수가 칸 수보다 적으면 남는 칸은 비워 둔다.
            _offers.Add(i < _pickBuffer.Count ? CreateOffer(_pickBuffer[i]) : null);
        }
    }

    private ShopOfferData CreateOffer(ShopCatalogSO.Item item)
    {
        float variance = _catalog.PriceVariance;
        float price = item.BasePrice * Random.Range(1f - variance, 1f + variance);
        return new ShopOfferData(item.Id, item.DisplayName, item.Description, item.Icon, item.Type,
            Mathf.Max(1, RoundPrice(price)), item.HealRatio);
    }

    private int RoundPrice(float price)
    {
        int unit = Mathf.Max(1, _catalog.PriceRoundUnit);
        return Mathf.RoundToInt(price / unit) * unit;
    }
}
