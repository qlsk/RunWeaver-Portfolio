using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 원소 반응 조합표의 칸 하나.
/// - 원소 칸(머리): 원소 색 바탕 + 흰색 원소 아이콘, 누르면 기본 원소 효과를 보여준다.
/// - 반응 칸: 두 원소 색을 섞은 바탕 + 두 원소 색 점 + 반응 이름, 누르면 반응 설명을 보여준다.
/// 선택되면 기본 테두리 대신 붉은 선택 테두리를 켠다.
/// 칸에 표시할 원소는 인스펙터에서 정한다.
/// </summary>
[DisallowMultipleComponent]
public class ElementReactionCellView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ReactionElement _elementA;
    [Tooltip("체크하면 기본 원소 칸(머리), 해제하면 두 원소의 반응 칸")]
    [SerializeField] private bool _isBaseElement;
    [SerializeField] private ReactionElement _elementB;

    [Header("구성 요소")]
    [Tooltip("칸 바탕 (흰색 스프라이트에 색을 입힌다)")]
    [SerializeField] private Image _background;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _dotA;
    [SerializeField] private Image _dotB;
    [SerializeField] private TMP_Text _nameText;
    [Tooltip("평소 테두리. 선택되면 끈다.")]
    [SerializeField] private GameObject _frame;
    [Tooltip("선택 테두리")]
    [SerializeField] private GameObject _selectedMark;

    [Header("반응 칸 바탕색 (종이색에 두 원소 색을 이 비율로 섞는다)")]
    [SerializeField] private Color _slotColor = new(0.894f, 0.804f, 0.678f, 1f);
    [SerializeField, Range(0f, 1f)] private float _elementTint = 0.28f;

    [Header("장착 중 강조")]
    [Tooltip("지금 장착한 원소 조합 칸에 두르는 테두리 색. 선택 테두리를 복제해 이 색으로 칠한다")]
    [SerializeField] private Color _equippedColor = new(1f, 0.8f, 0.32f, 1f);
    [SerializeField, Min(0f)] private float _equippedPulseSpeed = 3f;
    [Tooltip("장착 중일 때 켜는 표시. 비우면 선택 테두리를 복사해서 만든다")]
    [SerializeField] private GameObject _equippedMark;
    [Tooltip("깜빡일 그래픽. 비우면 장착 표시 안의 그래픽 전부")]
    [SerializeField] private UnityEngine.UI.Graphic[] _equippedPulseGraphics;

    private UnityEngine.UI.Graphic[] _equippedGraphics;
    private bool _isEquipped;

    public event Action<ElementReactionCellView> Clicked;

    public ReactionElement ElementA => _elementA;
    public ReactionElement ElementB => _elementB;
    public bool IsBaseElement => _isBaseElement;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked?.Invoke(this);
        }
    }

    public void Show(ElementReactionData data, Sprite elementIcon)
    {
        Color colorA = ReactionElementStyle.GetColor(_elementA);
        Color colorB = ReactionElementStyle.GetColor(_elementB);

        if (_isBaseElement)
        {
            SetColor(_background, colorA);
            if (_iconImage != null)
            {
                _iconImage.enabled = elementIcon != null;
                _iconImage.sprite = elementIcon;
            }

            SetActive(_dotA, false);
            SetActive(_dotB, false);
            if (_nameText != null)
            {
                _nameText.gameObject.SetActive(false);
            }

            return;
        }

        Color mixed = Color.Lerp(colorA, colorB, 0.5f);
        Color background = Color.Lerp(_slotColor, mixed, _elementTint);
        background.a = 1f;
        SetColor(_background, background);
        if (_iconImage != null)
        {
            _iconImage.enabled = false;
        }

        SetActive(_dotA, true);
        SetActive(_dotB, true);
        SetColor(_dotA, colorA);
        SetColor(_dotB, colorB);

        if (_nameText != null)
        {
            _nameText.gameObject.SetActive(true);
            _nameText.text = data != null ? data.DisplayName : "???";
        }
    }

    /// <summary>지금 장착한 원소 조합이면 금색 테두리를 두르고 은은하게 깜빡인다.</summary>
    public void SetEquipped(bool isEquipped)
    {
        _isEquipped = isEquipped;
        if (isEquipped)
        {
            EnsureEquippedMark();
        }

        if (_equippedMark != null)
        {
            _equippedMark.SetActive(isEquipped);
        }
    }

    private void Update()
    {
        if (!_isEquipped || _equippedGraphics == null)
        {
            return;
        }

        float alpha = 0.65f + 0.35f * (Mathf.Sin(Time.unscaledTime * _equippedPulseSpeed) * 0.5f + 0.5f);
        foreach (UnityEngine.UI.Graphic graphic in _equippedGraphics)
        {
            if (graphic != null)
            {
                Color color = _equippedColor;
                color.a *= alpha;
                graphic.color = color;
            }
        }
    }

    private void EnsureEquippedMark()
    {
        if (_equippedMark != null)
        {
            if (_equippedGraphics == null)
            {
                _equippedGraphics = _equippedPulseGraphics != null && _equippedPulseGraphics.Length > 0
                    ? _equippedPulseGraphics
                    : _equippedMark.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            }

            return;
        }

        GameObject source = _selectedMark != null ? _selectedMark : _frame;
        if (source == null)
        {
            return;
        }

        _equippedMark = Instantiate(source, source.transform.parent);
        _equippedMark.name = "EquippedMark";
        _equippedMark.transform.SetAsLastSibling();
        _equippedGraphics = _equippedMark.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        foreach (UnityEngine.UI.Graphic graphic in _equippedGraphics)
        {
            graphic.raycastTarget = false;
            graphic.color = _equippedColor;
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (_selectedMark != null)
        {
            _selectedMark.SetActive(isSelected);
        }

        if (_frame != null)
        {
            _frame.SetActive(!isSelected || _selectedMark == null);
        }
    }

    private static void SetColor(Image image, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }

    private static void SetActive(Image image, bool isActive)
    {
        if (image != null)
        {
            image.gameObject.SetActive(isActive);
        }
    }
}
