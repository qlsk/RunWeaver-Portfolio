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
