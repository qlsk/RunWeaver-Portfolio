using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상점 칸 하나 (예: Slot1). 칸을 누르면 선택되고, 연결된 Buy 버튼으로 구매한다.
/// 칸 배경은 판넬 이미지에 그려져 있어, 클릭 영역은 칸의 자식(투명 이미지 HitArea 등)이 받는다.
/// 골드가 부족하면 가격과 Buy 글자를 붉게 표시하고, 산 칸은 "SOLD"로 표시한다.
/// </summary>
[DisallowMultipleComponent]
public class ShopSlotView : MonoBehaviour, IPointerClickHandler
{
    [Header("칸 구성 요소")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private Image _iconImage;

    [Header("Buy 버튼 (판넬 이미지 위의 글자)")]
    [SerializeField] private Button _buyButton;
    [SerializeField] private TMP_Text _buyText;

    [Header("표시")]
    [SerializeField] private string _priceFormat = "({0})";
    [SerializeField] private string _soldText = "SOLD";
    [SerializeField] private Color _insufficientColor = new(0.85f, 0.15f, 0.12f, 1f);

    private Color _normalPriceColor = Color.white;
    private Color _normalBuyColor = Color.white;
    private Sprite _defaultIcon;
    private bool _hasOffer;
    private bool _isInitialized;

    /// <summary>칸을 눌렀을 때 (빈 칸은 발생하지 않는다)</summary>
    public event Action<ShopSlotView> Clicked;

    /// <summary>Buy를 눌렀을 때</summary>
    public event Action<ShopSlotView> BuyClicked;

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.RemoveListener(HandleBuyClicked);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && _hasOffer)
        {
            Clicked?.Invoke(this);
        }
    }

    public void Show(ShopOfferData offer, bool canAfford)
    {
        Initialize();
        if (offer == null)
        {
            ShowSold();
            return;
        }

        _hasOffer = true;
        SetText(_nameText, offer.DisplayName);
        SetText(_priceText, string.Format(_priceFormat, offer.Price.ToString("#,0", CultureInfo.InvariantCulture)));

        if (_iconImage != null)
        {
            _iconImage.enabled = true;
            _iconImage.sprite = offer.Icon != null ? offer.Icon : _defaultIcon;
        }

        Color color = canAfford ? _normalPriceColor : _insufficientColor;
        if (_priceText != null)
        {
            _priceText.color = color;
        }

        if (_buyButton != null)
        {
            _buyButton.gameObject.SetActive(true);
            _buyButton.interactable = canAfford;
        }

        if (_buyText != null)
        {
            _buyText.color = canAfford ? _normalBuyColor : _insufficientColor;
        }
    }

    /// <summary>산 칸 (또는 진열할 품목이 없는 칸)</summary>
    public void ShowSold()
    {
        Initialize();
        _hasOffer = false;
        SetText(_nameText, _soldText);
        SetText(_priceText, string.Empty);

        if (_iconImage != null)
        {
            _iconImage.enabled = false;
        }

        if (_buyButton != null)
        {
            _buyButton.gameObject.SetActive(false);
        }
    }

    private void HandleBuyClicked()
    {
        if (_hasOffer)
        {
            BuyClicked?.Invoke(this);
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        if (_priceText != null)
        {
            _normalPriceColor = _priceText.color;
        }

        if (_iconImage != null)
        {
            _defaultIcon = _iconImage.sprite;
        }

        if (_buyButton != null)
        {
            if (_buyText == null)
            {
                _buyText = _buyButton.GetComponentInChildren<TMP_Text>(true);
            }

            _buyButton.onClick.AddListener(HandleBuyClicked);
        }

        if (_buyText != null)
        {
            _normalBuyColor = _buyText.color;
        }
    }
}
