using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 UI(ShopUI)를 ShopDataSource와 연결한다.
/// - 상점이 새로 진열되면 첫 칸을 자동으로 선택해 Details에 표시한다.
/// - 선택한 칸을 사면 남은 칸 중 첫 칸을 선택한다. 남은 칸이 없으면 Details를 비운다.
/// - 골드가 부족하면 Reroll도 비활성화하고 글자를 붉게 표시한다.
/// - Leave를 누르면 상점 창을 닫는다. (다음 스테이지 진행은 Left 이벤트를 받는 쪽에서 연결)
/// 데이터 소스를 비워 두면 씬에서 찾는다.
/// </summary>
[DisallowMultipleComponent]
public class ShopPresenter : MonoBehaviour
{
    [SerializeField] private ShopDataSource _dataSource;
    [SerializeField] private ShopSlotView[] _slots;

    [Header("Details")]
    [SerializeField] private TMP_Text _detailNameText;
    [SerializeField] private TMP_Text _detailDescriptionText;
    [Tooltip("포션 설명 아래에 붙는 체력 변화. {0} 현재 체력, {1} 구매 후 체력, {2} 최대 체력")]
    [SerializeField] private string _potionHealthFormat = "({0}/{2}) -> <color=#7FD38A>({1}/{2})</color>";
    [Tooltip("체력이 가득 찼을 때 포션 설명 아래에 붙는 안내. 이때는 포션을 살 수 없다")]
    [SerializeField] private string _fullHealthText = "<color=#E07A7A>Health is already full.</color>";

    [Header("Reroll")]
    [SerializeField] private Button _rerollButton;
    [SerializeField] private TMP_Text _rerollText;
    [SerializeField] private string _rerollFormat = "Reroll - Cost <{0}>";

    [Header("Leave")]
    [SerializeField] private Button _leaveButton;
    [Tooltip("Leave를 누르면 끌 오브젝트. 비워두면 이 오브젝트를 끈다.")]
    [SerializeField] private GameObject _closeTarget;

    [Header("골드")]
    [SerializeField] private GoldHudView _goldView;

    [Header("색")]
    [SerializeField] private Color _insufficientColor = new(0.85f, 0.15f, 0.12f, 1f);

    private Color _normalRerollColor = Color.white;
    private int _selectedIndex = -1;

    /// <summary>Leave로 상점을 나갔을 때</summary>
    public event Action Left;

    private void Awake()
    {
        if (_dataSource == null)
        {
            _dataSource = FindAnyObjectByType<ShopDataSource>();
        }

        if (_dataSource == null)
        {
            Debug.LogError("[ShopPresenter] ShopDataSource를 찾을 수 없습니다.", this);
            enabled = false;
            return;
        }

        if (_rerollText != null)
        {
            _normalRerollColor = _rerollText.color;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                _slots[i].Clicked += HandleSlotClicked;
                _slots[i].BuyClicked += HandleBuyClicked;
            }
        }

        if (_rerollButton != null)
        {
            _rerollButton.onClick.AddListener(HandleRerollClicked);
        }

        if (_leaveButton != null)
        {
            _leaveButton.onClick.AddListener(HandleLeaveClicked);
        }

        _dataSource.Changed += Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        if (_dataSource != null)
        {
            _dataSource.Changed -= Refresh;
        }

        foreach (ShopSlotView slot in _slots)
        {
            if (slot != null)
            {
                slot.Clicked -= HandleSlotClicked;
                slot.BuyClicked -= HandleBuyClicked;
            }
        }

        if (_rerollButton != null)
        {
            _rerollButton.onClick.RemoveListener(HandleRerollClicked);
        }

        if (_leaveButton != null)
        {
            _leaveButton.onClick.RemoveListener(HandleLeaveClicked);
        }
    }

    /// <summary>상점을 새로 연다. (창을 켜고 리롤 비용을 초기화한 뒤 새로 진열)</summary>
    public void OpenShop()
    {
        GameObject target = _closeTarget != null ? _closeTarget : gameObject;
        target.SetActive(true);
        _selectedIndex = -1;
        _dataSource.Open();
    }

    private void Refresh()
    {
        if (_dataSource == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                continue;
            }

            ShopOfferData offer = _dataSource.GetOffer(i);
            if (offer != null)
            {
                _slots[i].Show(offer, _dataSource.CanAfford(offer.Price), _dataSource.CanPurchase(offer));
            }
            else
            {
                _slots[i].ShowSold();
            }
        }

        if (_dataSource.GetOffer(_selectedIndex) == null)
        {
            _selectedIndex = FindFirstOfferIndex();
        }

        ShowDetails();
        RefreshReroll();

        if (_goldView != null)
        {
            _goldView.SetAmount(_dataSource.Gold);
        }
    }

    private void RefreshReroll()
    {
        bool canReroll = _dataSource.CanReroll();
        if (_rerollButton != null)
        {
            _rerollButton.interactable = canReroll;
        }

        if (_rerollText != null)
        {
            _rerollText.text = string.Format(_rerollFormat,
                _dataSource.RerollCost.ToString("#,0", CultureInfo.InvariantCulture));
            _rerollText.color = canReroll ? _normalRerollColor : _insufficientColor;
        }
    }

    private void ShowDetails()
    {
        ShopOfferData offer = _dataSource.GetOffer(_selectedIndex);
        if (_detailNameText != null)
        {
            _detailNameText.text = offer != null ? offer.DisplayName : string.Empty;
        }

        if (_detailDescriptionText != null)
        {
            _detailDescriptionText.text = offer != null ? offer.Description + GetPotionHealthText(offer) : string.Empty;
        }
    }

    /// <summary>포션이면 "(현재/최대) -> (구매 후/최대)" 줄을 만든다. 체력을 알 수 없으면 빈 문자열.</summary>
    private string GetPotionHealthText(ShopOfferData offer)
    {
        if (offer.Type != ShopItemType.Potion || offer.HealRatio <= 0f ||
            !_dataSource.TryGetPlayerHealth(out float current, out float max))
        {
            return string.Empty;
        }

        if (current >= max)
        {
            return "\n\n" + _fullHealthText;
        }

        float after = Mathf.Min(max, current + max * offer.HealRatio);
        return "\n\n" + string.Format(CultureInfo.InvariantCulture, _potionHealthFormat,
            Mathf.RoundToInt(current), Mathf.RoundToInt(after), Mathf.RoundToInt(max));
    }

    private int FindFirstOfferIndex()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_dataSource.GetOffer(i) != null)
            {
                return i;
            }
        }

        return -1;
    }

    private void HandleSlotClicked(ShopSlotView slot)
    {
        int index = Array.IndexOf(_slots, slot);
        if (index < 0 || _dataSource.GetOffer(index) == null)
        {
            return;
        }

        _selectedIndex = index;
        ShowDetails();
    }

    private void HandleBuyClicked(ShopSlotView slot)
    {
        int index = Array.IndexOf(_slots, slot);
        if (index >= 0)
        {
            _dataSource.TryBuy(index);
        }
    }

    private void HandleRerollClicked()
    {
        if (_dataSource.TryReroll())
        {
            // 리롤하면 첫 칸을 다시 선택한다.
            _selectedIndex = FindFirstOfferIndex();
            ShowDetails();
        }
    }

    private void HandleLeaveClicked()
    {
        Debug.Log("[ShopPresenter] Leave shop", this);
        GameObject target = _closeTarget != null ? _closeTarget : gameObject;
        target.SetActive(false);
        SafeEventNotifier.Notify(Left);
    }
}
