using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 룬 글자 메뉴 항목 하나. (타이틀 메뉴, 설정 창 버튼에서 같이 쓴다)
/// 선택되면 글자가 밝아지며 살짝 옆으로 나오고, 양옆에 ">  <" 꺾쇠, 뒤에 푸른 빛, 아래에 룬 밑줄이 생긴다.
/// - 메뉴(TitleMenuView)가 있으면 선택은 메뉴가 정한다. (Hovered / Clicked 이벤트만 알린다)
/// - _selectOnHover를 켜면 혼자서 마우스 / 키보드 선택에 따라 선택 상태를 바꾼다. (설정 창 버튼)
/// - Appear(0~1)로 등장 연출, Press(0~)로 결정 연출을 한다.
/// 실제 시간으로 움직인다. (일시정지 중에도 동작)
/// </summary>
[DisallowMultipleComponent]
public class RuneMenuItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("구성 요소")]
    [Tooltip("선택될 때 옆으로 움직일 묶음 (글자, 꺾쇠, 빛, 밑줄의 부모)")]
    [SerializeField] private RectTransform _content;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private RectTransform _leftChevron;
    [SerializeField] private RectTransform _rightChevron;
    [SerializeField] private Graphic _glow;
    [SerializeField] private Graphic _underline;
    [SerializeField] private Graphic _underlineMark;
    [Tooltip("누를 수 없는 항목 옆의 작은 글자 (예: 기록 없음)")]
    [SerializeField] private TMP_Text _subText;

    [Header("상태")]
    [SerializeField] private bool _isDisabled;
    [Tooltip("혼자 쓸 때 켠다. 마우스를 올리거나 키보드로 고르면 스스로 선택된다")]
    [SerializeField] private bool _selectOnHover;

    [Header("색")]
    [SerializeField] private Color _selectedColor = new(0.925f, 0.902f, 0.863f, 1f);
    [SerializeField] private Color _normalColor = new(0.659f, 0.69f, 0.761f, 1f);
    [SerializeField] private Color _disabledColor = new(0.43f, 0.455f, 0.518f, 1f);
    [SerializeField] private Color _accentColor = new(0.588f, 0.765f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.6f;

    [Header("움직임")]
    [SerializeField] private float _selectedOffset = 10f;
    [SerializeField] private float _chevronGap = 22f;
    [SerializeField] private float _chevronSlide = 14f;
    [SerializeField] private float _underlineOffset = 22f;
    [SerializeField] private float _appearRise = 14f;
    [SerializeField] private float _breathAmount = 3f;
    [SerializeField] private float _breathSpeed = 4f;
    [Tooltip("결정할 때 꺾쇠가 안쪽으로 모이는 거리")]
    [SerializeField] private float _pressPinch = 18f;
    [SerializeField, Min(0f)] private float _sharpness = 14f;

    private float _selection;
    private Vector2 _contentBase;
    private bool _isInitialized;

    public event Action<RuneMenuItemView> Hovered;
    public event Action<RuneMenuItemView> Clicked;

    public bool IsSelected { get; set; }
    public bool IsDisabled => _isDisabled;

    /// <summary>항목 글자</summary>
    public string Label
    {
        get => _text != null ? _text.text : string.Empty;
        set
        {
            if (_text != null)
            {
                _text.text = value;
            }
        }
    }

    /// <summary>누를 수 있는지 바꾼다. (타이틀의 이어하기)</summary>
    public void SetDisabled(bool isDisabled)
    {
        Initialize();
        _isDisabled = isDisabled;
        Apply();
    }

    /// <summary>옆의 작은 글자를 바꾼다. 작은 글자가 없는 항목이면 아무것도 하지 않는다.</summary>
    public void SetSubText(string text)
    {
        if (_subText != null)
        {
            _subText.text = text;
        }
    }

    /// <summary>옆의 작은 글자. 없으면 빈 문자열</summary>
    public string SubText => _subText != null ? _subText.text : string.Empty;

    /// <summary>등장 정도 (0 숨김 ~ 1 다 나옴)</summary>
    public float Appear { get; set; } = 1f;

    /// <summary>결정 연출 진행 시간 (0이면 연출 없음)</summary>
    public float Press { get; set; }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        _selection = IsSelected ? 1f : 0f;
        Apply();
    }

    public void SnapSelection()
    {
        Initialize();
        _selection = IsSelected ? 1f : 0f;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_selectOnHover && !_isDisabled)
        {
            IsSelected = true;
            // 키보드로 고른 항목과 둘 다 선택돼 보이지 않게, 마우스를 올린 항목을 선택 항목으로 만든다.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        Hovered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_selectOnHover && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
        {
            IsSelected = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked?.Invoke(this);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_selectOnHover && !_isDisabled)
        {
            IsSelected = true;
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (_selectOnHover)
        {
            IsSelected = false;
        }
    }

    private void Update()
    {
        float target = IsSelected && !_isDisabled ? 1f : 0f;
        _selection = Mathf.Lerp(_selection, target, 1f - Mathf.Exp(-_sharpness * Time.unscaledDeltaTime));
        Apply();
    }

    private void Apply()
    {
        float t = _selection;
        float appear = Mathf.Clamp01(Appear);
        float press = Mathf.Max(0f, Press);
        float whiten = Mathf.Clamp01(press * 3f);

        if (_content != null)
        {
            _content.anchoredPosition = _contentBase + new Vector2(_selectedOffset * t, -(1f - appear) * _appearRise);
        }

        if (_text != null)
        {
            Color color = _isDisabled ? _disabledColor : Color.Lerp(_normalColor, _selectedColor, t);
            color = Color.Lerp(color, Color.white, whiten * t);
            color.a *= appear;
            _text.color = color;
        }

        if (_subText != null)
        {
            Color color = _subText.color;
            color.a = appear * 0.85f;
            _subText.color = color;
        }

        GetTextBounds(out float xMin, out float xMax, out float y);
        float width = xMax - xMin;
        float center = (xMin + xMax) * 0.5f;
        float shown = t * appear;

        if (_glow != null)
        {
            Color color = _accentColor;
            color.a = _glowAlpha * shown * (1f + whiten);
            _glow.color = color;
            _glow.rectTransform.anchoredPosition = new Vector2(center, y);
            _glow.rectTransform.sizeDelta = new Vector2(width + 90f, _glow.rectTransform.sizeDelta.y);
        }

        if (_underline != null)
        {
            Color color = _accentColor;
            color.a = 0.8f * shown;
            _underline.color = color;
            _underline.rectTransform.anchoredPosition = new Vector2(center, y - _underlineOffset);
            _underline.rectTransform.sizeDelta = new Vector2((width + 8f) * t, _underline.rectTransform.sizeDelta.y);
        }

        if (_underlineMark != null)
        {
            Color color = Color.Lerp(_accentColor, Color.white, 0.3f);
            color.a = shown;
            _underlineMark.color = color;
            _underlineMark.rectTransform.anchoredPosition = new Vector2(center, y - _underlineOffset);
        }

        float breath = Mathf.Sin(Time.unscaledTime * _breathSpeed) * _breathAmount * t;
        float pinch = Mathf.Min(press, 1f) * _pressPinch;
        float slide = _chevronSlide * (1f - t);
        PlaceChevron(_leftChevron, xMin - _chevronGap - slide - breath + pinch, y, shown);
        PlaceChevron(_rightChevron, xMax + _chevronGap + slide + breath - pinch, y, shown);
    }

    private void PlaceChevron(RectTransform chevron, float x, float y, float alpha)
    {
        if (chevron == null)
        {
            return;
        }

        chevron.anchoredPosition = new Vector2(x, y);
        Graphic graphic = chevron.GetComponent<Graphic>();
        if (graphic != null)
        {
            Color color = Color.Lerp(_accentColor, Color.white, 0.15f);
            color.a = alpha;
            graphic.color = color;
        }
    }

    /// <summary>글자가 실제로 차지하는 가로 범위와 세로 중심 (_content 기준)</summary>
    private void GetTextBounds(out float xMin, out float xMax, out float y)
    {
        xMin = xMax = y = 0f;
        if (_text == null)
        {
            return;
        }

        RectTransform rect = _text.rectTransform;
        float width = _text.GetPreferredValues(_text.text).x;
        float left = rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
        HorizontalAlignmentOptions align = _text.horizontalAlignment;
        if (align == HorizontalAlignmentOptions.Left)
        {
            xMin = left;
        }
        else if (align == HorizontalAlignmentOptions.Right)
        {
            xMin = left + rect.rect.width - width;
        }
        else
        {
            xMin = left + (rect.rect.width - width) * 0.5f;
        }

        xMax = xMin + width;
        y = rect.anchoredPosition.y + (0.5f - rect.pivot.y) * rect.rect.height;
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_content != null)
        {
            _contentBase = _content.anchoredPosition;
        }
    }
}
