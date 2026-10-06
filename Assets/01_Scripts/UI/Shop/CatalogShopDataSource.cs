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
/// </summary>
public class CatalogShopDataSource : ShopDataSource
{
    [SerializeField] private ShopCatalogSO _catalog;
    [Tooltip("원정 지갑이 없을 때(단독 테스트) 쓰는 시작 골드")]
    [SerializeField, Min(0)] private int _startingGold = 5000;
    [SerializeField] private bool _openOnStart = true;

    [Header("테스트용 플레이어 체력 (씬에 PlayerHealth가 없을 때 사용)")]
    [SerializeField, Min(0f)] private float _testCurrentHealth = 45f;
    [SerializeField, Min(1f)] private float _testMaxHealth = 100f;

    private readonly List<ShopOfferData> _offers = new();
    private readonly List<ShopCatalogSO.Item> _pickBuffer = new();
    private int _gold;
    private int _rerollCount;

    public override IReadOnlyList<ShopOfferData> Offers => _offers;

    private Wallet _boundWallet;

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
        if (_openOnStart)
        {
            Open();
        }
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
        if (!TrySpend(offer.Price))
        {
            return false;
        }

        _offers[slotIndex] = null;
        GrantItem(offer);

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

    /// <summary>산 품목을 지급한다. (지금은 룬 워드 완성품만. 나머지 종류는 각 시스템 연결 후 추가)</summary>
    private void GrantItem(ShopOfferData offer)
    {
        if (offer.Type != ShopItemType.RuneWord)
        {
            return;
        }

        ShopCatalogSO.Item item = FindItem(offer.Id);
        if (item == null || string.IsNullOrEmpty(item.RuneEffectId))
        {
            Debug.LogWarning($"[Shop] 룬 워드 품목 {offer.Id}에 runeEffectId가 없어 지급하지 못했습니다.", this);
            return;
        }

        OwnedRuneWords.Add(item.RuneEffectId);
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
            Mathf.Max(1, RoundPrice(price)), item.RealRatio);
    }

    private int RoundPrice(float price)
    {
        int unit = Mathf.Max(1, _catalog.PriceRoundUnit);
        return Mathf.RoundToInt(price / unit) * unit;
    }
}
