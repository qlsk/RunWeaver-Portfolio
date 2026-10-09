using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 엔딩 메뉴 글자 하나. ("다음 개체 투입", "타이틀로 돌아가기")
/// 선택되면 양옆에 ">  <" 꺾쇠가 안쪽으로 미끄러져 들어오고, 글자가 밝고 커지며 밑줄이 굵어진다.
/// 선택된 동안 꺾쇠는 천천히 숨 쉬듯 좌우로 움직인다. 실제 시간으로 움직인다.
/// 마우스를 올리면 선택되고, 누르면 실행한다. (EndingMenuView가 받는다)
/// </summary>
[DisallowMultipleComponent]
public class EndingMenuItemView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Image _underline;
    [Tooltip("왼쪽 꺾쇠 ( > )")]
    [SerializeField] private RectTransform _leftChevron;
    [Tooltip("오른쪽 꺾쇠 ( < )")]
    [SerializeField] private RectTransform _rightChevron;
    [Tooltip("선택됐을 때 글자 뒤에 번지는 빛")]
    [SerializeField] private Graphic _glow;

    [Header("색")]
    [SerializeField] private Color _selectedTextColor = new(0.925f, 0.878f, 0.784f, 1f);
    [SerializeField] private Color _normalTextColor = new(0.588f, 0.549f, 0.486f, 1f);
    [SerializeField] private Color _selectedLineColor = new(0.69f, 0.588f, 0.431f, 1f);
    [SerializeField] private Color _normalLineColor = new(0.376f, 0.353f, 0.314f, 1f);
    [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.55f;

    [Header("움직임")]
    [SerializeField] private float _selectedScale = 1.07f;
    [Tooltip("글자 끝에서 꺾쇠까지의 거리")]
    [SerializeField] private float _chevronGap = 30f;
    [Tooltip("선택될 때 꺾쇠가 바깥에서 미끄러져 들어오는 거리")]
    [SerializeField] private float _chevronSlide = 16f;
    [SerializeField] private float _breathAmount = 2f;
    [SerializeField] private float _breathSpeed = 3.2f;
    [SerializeField, Min(0f)] private float _sharpness = 14f;

    private bool _isSelected;
    private float _selection;
    private Graphic _leftGraphic;
    private Graphic _rightGraphic;
    private Vector3 _textBaseScale = Vector3.one;
    private bool _isInitialized;

    public event Action<EndingMenuItemView> Hovered;
    public event Action<EndingMenuItemView> Clicked;

    public bool IsSelected
    {
        get => _isSelected;
        set => _isSelected = value;
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        _selection = _isSelected ? 1f : 0f;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hovered?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked?.Invoke(this);
        }
    }

    /// <summary>연출 없이 바로 선택 상태를 맞춘다.</summary>
    public void SnapSelection()
    {
        Initialize();
        _selection = _isSelected ? 1f : 0f;
        Apply();
    }

    private void Update()
    {
        _selection = Mathf.Lerp(_selection, _isSelected ? 1f : 0f, ShopEase.Damp(_sharpness, Time.unscaledDeltaTime));
        Apply();
    }

    private void Apply()
    {
        float t = _selection;
        if (_text != null)
        {
            _text.color = Color.Lerp(_normalTextColor, _selectedTextColor, t);
            _text.rectTransform.localScale = _textBaseScale * Mathf.Lerp(1f, _selectedScale, t);
        }

        float halfWidth = GetTextHalfWidth() * Mathf.Lerp(1f, _selectedScale, t);

        if (_underline != null)
        {
            _underline.color = Color.Lerp(_normalLineColor, _selectedLineColor, t);
            RectTransform line = _underline.rectTransform;
            line.sizeDelta = new Vector2(halfWidth * 2f, Mathf.Lerp(1f, 2f, t));
        }

        if (_glow != null)
        {
            Color color = _glow.color;
            color.a = _glowAlpha * t;
            _glow.color = color;
        }

        float breath = Mathf.Sin(Time.unscaledTime * _breathSpeed) * _breathAmount * t;
        float x = halfWidth + _chevronGap + _chevronSlide * (1f - t) + breath;
        PlaceChevron(_leftChevron, _leftGraphic, -x, t);
        PlaceChevron(_rightChevron, _rightGraphic, x, t);
    }

    private static void PlaceChevron(RectTransform chevron, Graphic graphic, float x, float alpha)
    {
        if (chevron == null)
        {
            return;
        }

        chevron.anchoredPosition = new Vector2(x, chevron.anchoredPosition.y);
        if (graphic != null)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }

    private float GetTextHalfWidth()
    {
        return _text != null ? _text.GetPreferredValues(_text.text).x * 0.5f : 0f;
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_text != null)
        {
            _textBaseScale = _text.rectTransform.localScale;
        }

        _leftGraphic = _leftChevron != null ? _leftChevron.GetComponent<Graphic>() : null;
        _rightGraphic = _rightChevron != null ? _rightChevron.GetComponent<Graphic>() : null;
    }
}
