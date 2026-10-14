using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상점 칸 하나 (예: Slot1). 칸을 누르면 선택되고, 연결된 Buy 버튼으로 구매한다.
/// 칸 테두리(_frameImage)와 아이콘 상자는 칸의 자식 이미지이고, 클릭은 칸 전체를 덮는 투명 이미지(HitArea)가 받는다.
/// 선택된 칸은 테두리가 선택용 스프라이트(금테)로 바뀐다.
/// 골드가 부족하면 가격과 Buy 글자를 붉게 표시하고, 산 칸은 "판매 완료"로 표시한다.
/// 골드가 부족해도 Buy는 눌린다. (상점 쪽에서 거절 연출을 하기 위해서)
///
/// 연출 (모두 실제 시간으로 움직인다. 상점이 열린 동안 게임이 멈춰 있기 때문)
/// - PlayAppear : 상점이 열릴 때 칸이 튀어나온다.
/// - PlayFlip   : 새로고침할 때 칸이 뒤집히고, 반쯤 뒤집혔을 때 새 품목으로 바뀐다.
/// - PlayPurchase / PlayDenied : 구매 성공 시 통 튀고, 실패 시 좌우로 흔들린다.
/// - 산 칸은 "판매 완료" 글자가 도장처럼 찍힌다.
/// - 마우스를 올리거나 선택하면 살짝 커지고, 아이콘은 천천히 위아래로 떠다닌다.
/// </summary>
[DisallowMultipleComponent]
public class ShopSlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("칸 구성 요소")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private Image _iconImage;

    [Header("칸 이미지")]
    [Tooltip("칸 테두리 이미지. 선택되면 선택용 스프라이트로 바뀐다")]
    [SerializeField] private Image _frameImage;
    [SerializeField] private Sprite _selectedFrameSprite;

    [Header("Buy 버튼")]
    [SerializeField] private Button _buyButton;
    [SerializeField] private TMP_Text _buyText;
    [Tooltip("Buy 글자 뒤의 상자. 글자와 같이 움직이고, 팔린 칸에서는 흐려진다")]
    [SerializeField] private RectTransform _buyBackground;
    [SerializeField, Range(0f, 1f)] private float _soldBuyBackgroundAlpha = 0.45f;

    [Header("표시")]
    [SerializeField] private string _priceFormat = "({0})";
    [SerializeField] private string _soldText = "판매 완료";
    [SerializeField] private Color _insufficientColor = new(0.85f, 0.15f, 0.12f, 1f);

    [Header("연출")]
    [Tooltip("커지거나 뒤집힐 때의 중심. 비워두면 칸 위치를 중심으로 쓴다 (보통 HitArea)")]
    [SerializeField] private RectTransform _animationCenter;
    [SerializeField, Min(0.01f)] private float _appearDuration = 0.38f;
    [Tooltip("나타날 때 시작 크기")]
    [SerializeField, Range(0f, 1f)] private float _appearStartScale = 0.55f;
    [SerializeField, Min(0.01f)] private float _flipDuration = 0.34f;
    [SerializeField, Min(0.01f)] private float _purchaseDuration = 0.28f;
    [SerializeField] private float _purchasePunch = 0.14f;
    [SerializeField, Min(0.01f)] private float _deniedDuration = 0.38f;
    [SerializeField] private float _deniedShake = 11f;
    [SerializeField, Min(0.01f)] private float _stampDuration = 0.3f;
    [SerializeField] private float _stampStartScale = 1.9f;
    [SerializeField] private float _hoverScale = 1.03f;
    [SerializeField] private float _selectedScale = 1.06f;
    [SerializeField, Min(0f)] private float _scaleSharpness = 16f;
    [Tooltip("아이콘이 위아래로 떠다니는 높이")]
    [SerializeField] private float _iconBobAmount = 3f;
    [SerializeField] private float _iconBobSpeed = 2.2f;

    private Color _normalPriceColor = Color.white;
    private Color _normalBuyColor = Color.white;
    private Sprite _defaultIcon;
    private bool _hasOffer;
    private bool _isInitialized;

    private RectTransform _rect;
    private Vector2 _basePosition;
    private CanvasGroup _canvasGroup;
    private RectTransform _buyRect;
    private Vector2 _buyBasePosition;
    private CanvasGroup _buyGroup;
    private Vector2 _buyBackgroundBasePosition;
    private CanvasGroup _buyBackgroundGroup;
    private Sprite _normalFrameSprite;
    private RectTransform _iconRect;
    private Vector2 _iconBasePosition;
    private RectTransform _nameRect;
    private Vector3 _nameBaseScale = Vector3.one;
    private float _bobPhase;

    // 각 연출의 경과 시간. 음수면 대기 중(지연), NaN이면 재생하지 않는 중
    private float _appearTime = float.NaN;
    private float _flipTime = float.NaN;
    private bool _appearSoundPlayed;
    private bool _flipSoundPlayed;
    private float _purchaseTime = float.NaN;
    private float _deniedTime = float.NaN;
    private float _stampTime = float.NaN;

    private bool _flipApplied;
    private bool _deferDisplay;
    private Action _pendingDisplay;
    private bool _isHovered;
    private bool _isSelected;
    private float _holdScale = 1f;

    /// <summary>칸을 눌렀을 때 (빈 칸은 발생하지 않는다)</summary>
    public event Action<ShopSlotView> Clicked;

    /// <summary>Buy를 눌렀을 때 (골드가 부족해도 발생한다)</summary>
    public event Action<ShopSlotView> BuyClicked;

    public bool HasOffer => _hasOffer;

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        // 창이 꺼지면 연출을 끝난 상태로 정리해 둔다.
        StopAllEffects();
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
    }

    public void Show(ShopOfferData offer, bool canAfford)
    {
        Show(offer, canAfford, true);
    }

    /// <param name="canAfford">골드가 충분한지. (가격 색)</param>
    /// <param name="isAvailable">골드와 상관없이 살 수 있는 품목인지. (예: 체력이 가득 찬 상태의 포션은 false)</param>
    public void Show(ShopOfferData offer, bool canAfford, bool isAvailable)
    {
        Initialize();
        if (offer == null)
        {
            ShowSold();
            return;
        }

        if (_deferDisplay)
        {
            // 뒤집히는 중이면 반쯤 뒤집혔을 때 바꾼다.
            _pendingDisplay = () => ApplyOffer(offer, canAfford, isAvailable);
            return;
        }

        ApplyOffer(offer, canAfford, isAvailable);
    }

    /// <summary>산 칸 (또는 진열할 품목이 없는 칸)</summary>
    public void ShowSold()
    {
        Initialize();
        if (_deferDisplay)
        {
            _pendingDisplay = () => ApplySold(false);
            return;
        }

        // 품목이 있던 칸이 팔린 거라면 도장을 찍는다.
        ApplySold(_hasOffer && isActiveAndEnabled);
    }

    public void SetSelected(bool selected)
    {
        Initialize();
        _isSelected = selected;
        if (_frameImage != null)
        {
            _frameImage.sprite = selected && _selectedFrameSprite != null ? _selectedFrameSprite : _normalFrameSprite;
        }
    }

    /// <summary>상점이 열릴 때 delay초 뒤에 튀어나온다.</summary>
    public void PlayAppear(float delay)
    {
        Initialize();
        StopAllEffects();
        _appearTime = -Mathf.Max(0f, delay);
        _appearSoundPlayed = false;
        _holdScale = 1f;
        ApplyVisual();
    }

    /// <summary>delay초 뒤에 뒤집힌다. 그 사이에 들어온 Show / ShowSold는 반쯤 뒤집혔을 때 적용된다.</summary>
    public void PlayFlip(float delay)
    {
        Initialize();
        if (!isActiveAndEnabled)
        {
            return;
        }

        FlushPending();
        _flipTime = -Mathf.Max(0f, delay);
        _flipApplied = false;
        _flipSoundPlayed = false;
        _deferDisplay = true;
    }

    public void PlayPurchase()
    {
        if (isActiveAndEnabled)
        {
            _purchaseTime = 0f;
        }
    }

    /// <summary>살 수 없을 때 좌우로 흔들린다.</summary>
    public void PlayDenied()
    {
        if (isActiveAndEnabled)
        {
            _deniedTime = 0f;
        }
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        Advance(ref _appearTime, _appearDuration, deltaTime);
        if (!_appearSoundPlayed && !float.IsNaN(_appearTime) && _appearTime >= 0f)
        {
            _appearSoundPlayed = true;
            Sfx.PlayShopCardDeal();
        }
        Advance(ref _purchaseTime, _purchaseDuration, deltaTime);
        Advance(ref _deniedTime, _deniedDuration, deltaTime);
        Advance(ref _stampTime, _stampDuration, deltaTime);

        if (!float.IsNaN(_flipTime))
        {
            _flipTime += deltaTime;
            if (!_flipSoundPlayed && _flipTime >= 0f)
            {
                _flipSoundPlayed = true;
                Sfx.Play(Sfx.ShopFlip);
            }
            if (!_flipApplied && _flipTime >= _flipDuration * 0.5f)
            {
                _flipApplied = true;
                _deferDisplay = false;
                FlushPending();
            }

            if (_flipTime >= _flipDuration)
            {
                _flipTime = float.NaN;
                _deferDisplay = false;
                FlushPending();
            }
        }

        float targetHold = !_hasOffer ? 1f : _isSelected ? _selectedScale : _isHovered ? _hoverScale : 1f;
        _holdScale = Mathf.Lerp(_holdScale, targetHold, ShopEase.Damp(_scaleSharpness, deltaTime));

        ApplyVisual();
    }

    private static void Advance(ref float time, float duration, float deltaTime)
    {
        if (float.IsNaN(time))
        {
            return;
        }

        time += deltaTime;
        if (time >= duration)
        {
            time = float.NaN;
        }
    }

    private void ApplyVisual()
    {
        if (_rect == null)
        {
            return;
        }

        float scale = _holdScale;
        float alpha = 1f;

        if (!float.IsNaN(_appearTime))
        {
            if (_appearTime < 0f)
            {
                alpha = 0f;
                scale *= _appearStartScale;
            }
            else
            {
                float t = _appearTime / _appearDuration;
                scale *= Mathf.LerpUnclamped(_appearStartScale, 1f, ShopEase.OutBack(t));
                alpha = ShopEase.OutCubic(t * 1.6f);
            }
        }

        if (!float.IsNaN(_purchaseTime))
        {
            scale *= 1f + _purchasePunch * ShopEase.Bump(_purchaseTime / _purchaseDuration);
        }

        float flipX = 1f;
        if (!float.IsNaN(_flipTime) && _flipTime > 0f)
        {
            flipX = Mathf.Abs(Mathf.Cos(Mathf.Clamp01(_flipTime / _flipDuration) * Mathf.PI));
            flipX = Mathf.Max(0.02f, flipX);
        }

        Vector2 shake = Vector2.zero;
        if (!float.IsNaN(_deniedTime))
        {
            float t = _deniedTime / _deniedDuration;
            shake.x = Mathf.Sin(t * Mathf.PI * 7f) * _deniedShake * (1f - t);
        }

        Vector2 scaleVector = new(scale * flipX, scale);
        Vector2 center = _animationCenter != null ? (Vector2)_animationCenter.localPosition : Vector2.zero;

        // 칸의 기준점이 아닌 내용의 중심을 기준으로 커지도록 위치를 보정한다.
        _rect.localScale = new Vector3(scaleVector.x, scaleVector.y, 1f);
        _rect.anchoredPosition = _basePosition + Vector2.Scale(Vector2.one - scaleVector, center) + shake;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = alpha;
        }

        if (_buyRect != null)
        {
            _buyRect.localScale = new Vector3(scaleVector.x, scaleVector.y, 1f);
            _buyRect.anchoredPosition = _buyBasePosition + shake;
        }

        if (_buyGroup != null)
        {
            _buyGroup.alpha = alpha;
        }

        if (_buyBackground != null)
        {
            _buyBackground.localScale = new Vector3(scaleVector.x, scaleVector.y, 1f);
            _buyBackground.anchoredPosition = _buyBackgroundBasePosition + shake;
        }

        if (_buyBackgroundGroup != null)
        {
            _buyBackgroundGroup.alpha = alpha * (_hasOffer ? 1f : _soldBuyBackgroundAlpha);
        }

        if (_iconRect != null)
        {
            float bob = _hasOffer ? Mathf.Sin(Time.unscaledTime * _iconBobSpeed + _bobPhase) * _iconBobAmount : 0f;
            _iconRect.anchoredPosition = _iconBasePosition + new Vector2(0f, bob);
        }

        if (_nameRect != null)
        {
            float stampScale = 1f;
            if (!float.IsNaN(_stampTime))
            {
                stampScale = Mathf.Lerp(_stampStartScale, 1f, ShopEase.OutCubic(_stampTime / _stampDuration));
            }

            _nameRect.localScale = _nameBaseScale * stampScale;
            if (_nameText != null)
            {
                Color color = _nameText.color;
                color.a = float.IsNaN(_stampTime) ? 1f : Mathf.Clamp01(_stampTime / (_stampDuration * 0.5f));
                _nameText.color = color;
            }
        }
    }

    private void StopAllEffects()
    {
        _appearTime = float.NaN;
        _flipTime = float.NaN;
        _purchaseTime = float.NaN;
        _deniedTime = float.NaN;
        _stampTime = float.NaN;
        _deferDisplay = false;
        FlushPending();
        _isHovered = false;
        _holdScale = 1f;
        ApplyVisual();
    }

    private void FlushPending()
    {
        Action pending = _pendingDisplay;
        _pendingDisplay = null;
        pending?.Invoke();
    }

    private void ApplyOffer(ShopOfferData offer, bool canAfford, bool isAvailable)
    {
        _hasOffer = true;
        _stampTime = float.NaN;
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
            // 살 수 없어도 눌리게 둔다. 거절 연출은 상점 쪽에서 한다.
            _buyButton.gameObject.SetActive(true);
            _buyButton.interactable = true;
        }

        if (_buyText != null)
        {
            _buyText.color = canAfford && isAvailable ? _normalBuyColor : _insufficientColor;
        }
    }

    private void ApplySold(bool stamp)
    {
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

        _stampTime = stamp ? 0f : float.NaN;
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
        _rect = transform as RectTransform;
        if (_rect != null)
        {
            _basePosition = _rect.anchoredPosition;
        }

        _canvasGroup = GetOrAddCanvasGroup(gameObject);
        _bobPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        if (_priceText != null)
        {
            _normalPriceColor = _priceText.color;
        }

        if (_iconImage != null)
        {
            _defaultIcon = _iconImage.sprite;
            _iconRect = _iconImage.rectTransform;
            _iconBasePosition = _iconRect.anchoredPosition;
        }

        if (_nameText != null)
        {
            _nameRect = _nameText.rectTransform;
            _nameBaseScale = _nameRect.localScale;
        }

        if (_buyButton != null)
        {
            if (_buyText == null)
            {
                _buyText = _buyButton.GetComponentInChildren<TMP_Text>(true);
            }

            _buyButton.onClick.AddListener(HandleBuyClicked);
            _buyRect = _buyButton.transform as RectTransform;
            if (_buyRect != null)
            {
                _buyBasePosition = _buyRect.anchoredPosition;
            }

            _buyGroup = GetOrAddCanvasGroup(_buyButton.gameObject);
        }

        if (_buyText != null)
        {
            _normalBuyColor = _buyText.color;
        }

        if (_buyBackground != null)
        {
            _buyBackgroundBasePosition = _buyBackground.anchoredPosition;
            _buyBackgroundGroup = GetOrAddCanvasGroup(_buyBackground.gameObject);
            _buyBackgroundGroup.blocksRaycasts = false;
        }

        if (_frameImage != null)
        {
            _normalFrameSprite = _frameImage.sprite;
        }
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
    }
}
