using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 새 룬북(RuneBookV2)의 Rune Words 목록 칸 하나.
/// 표시할 룬 효과 id는 인스펙터에서 정한다. (칸 배치는 씬에서 카테고리별로 고정)
/// - 공개 단계(Listed / Detailed)에 따라 바탕색과 아이콘 색을 바꾼다.
/// - 원소 룬은 아이콘을 원소 색으로 칠하고, 장착 중이면 안쪽을 원소 색으로 채운다.
/// - 장착 중인 룬은 오른쪽 위에 금빛 핀을 꽂는다.
/// - 마우스를 올리면 빛과 붉은 테두리를 켜고, 왼쪽 클릭을 알린다.
/// - 상점에서 산 완성품이 있으면 오른쪽 위에 보유 수 표시(x1)를 켠다.
/// </summary>
[DisallowMultipleComponent]
public class RuneWordTileView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private string _entryId;

    [Header("구성 요소")]
    [SerializeField] private Image _background;
    [Tooltip("원소 룬일 때 원소 색으로 채우는 안쪽 바탕")]
    [SerializeField] private Image _elementFill;
    [SerializeField] private Image _icon;
    [Tooltip("평소 테두리. 마우스를 올리면 끈다.")]
    [SerializeField] private GameObject _frame;
    [SerializeField] private GameObject _hoverFrame;
    [SerializeField] private GameObject _hoverGlow;
    [Tooltip("장착 중일 때 켜는 표시 (금빛 핀)")]
    [SerializeField] private GameObject _equippedMark;

    [Header("완성품 보유 표시")]
    [SerializeField] private GameObject _ownedBadge;
    [SerializeField] private TMP_Text _ownedCountText;
    [SerializeField] private string _ownedFormat = "x{0}";

    [Header("공개 단계별 바탕색")]
    [SerializeField] private Color _detailedColor = new(0.894f, 0.804f, 0.678f, 1f);
    [SerializeField] private Color _listedColor = new(0.839f, 0.776f, 0.675f, 1f);
    [SerializeField] private Color _hiddenColor = new(0.431f, 0.337f, 0.282f, 1f);

    [Header("공개 단계별 아이콘 색")]
    [SerializeField] private Color _detailedIconColor = Color.white;
    [SerializeField] private Color _listedIconColor = new(0.667f, 0.627f, 0.588f, 1f);
    [SerializeField] private Color _hiddenIconColor = new(0.275f, 0.216f, 0.188f, 1f);
    [Tooltip("원소 색으로 채운 칸 위의 아이콘 색")]
    [SerializeField] private Color _onFillIconColor = new(0.98f, 0.94f, 0.88f, 1f);

    private bool _isHovered;

    public event Action<RuneWordTileView> PointerEntered;
    public event Action<RuneWordTileView> PointerExited;
    public event Action<RuneWordTileView> Clicked;

    /// <summary>우클릭 : 장착 칸에 들어 있는 이 룬 워드를 뺀다.</summary>
    public event Action<RuneWordTileView> RightClicked;

    public string EntryId => _entryId;
    public RuneBookEntryData Entry { get; private set; }
    public RectTransform RectTransform => (RectTransform)transform;

    private void OnEnable()
    {
        SetHovered(false);
    }

    private void OnDisable()
    {
        // 탭이 바뀌어 꺼지면 PointerExit가 오지 않으므로 여기서 알린다.
        // (꺼지는 중에는 자식 오브젝트를 켜고 끄지 않는다. 표시는 다시 켜질 때 OnEnable에서 되돌린다)
        if (_isHovered)
        {
            _isHovered = false;
            PointerExited?.Invoke(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHovered(true);
        PointerEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(false);
        PointerExited?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked?.Invoke(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            RightClicked?.Invoke(this);
        }
    }

    /// <param name="entry">표시할 룬 효과. null이면 칸을 숨긴다.</param>
    /// <param name="elementColor">원소 룬이면 원소 색, 아니면 null</param>
    /// <param name="ownedCount">남은 완성품 수 (0이면 표시를 끈다)</param>
    /// <param name="isEquipped">장착 칸에 들어 있는지 (확정 전 포함)</param>
    public void Show(RuneBookEntryData entry, Color? elementColor, int ownedCount = 0, bool isEquipped = false)
    {
        Entry = entry;
        if (gameObject.activeSelf != (entry != null))
        {
            gameObject.SetActive(entry != null);
        }

        if (entry == null)
        {
            return;
        }

        RuneWordRevealLevel level = entry.RevealLevel;
        bool isElement = elementColor.HasValue && level == RuneWordRevealLevel.Detailed;
        bool showFill = isElement && isEquipped;

        if (_equippedMark != null && _equippedMark.activeSelf != isEquipped)
        {
            _equippedMark.SetActive(isEquipped);
        }

        if (_background != null)
        {
            _background.color = level switch
            {
                RuneWordRevealLevel.Detailed => _detailedColor,
                RuneWordRevealLevel.Listed => _listedColor,
                _ => _hiddenColor
            };
        }

        if (_elementFill != null)
        {
            _elementFill.gameObject.SetActive(showFill);
            if (showFill)
            {
                _elementFill.color = elementColor.Value;
            }
        }

        if (_ownedBadge != null)
        {
            bool showOwned = ownedCount > 0;
            if (_ownedBadge.activeSelf != showOwned)
            {
                _ownedBadge.SetActive(showOwned);
            }

            if (showOwned && _ownedCountText != null)
            {
                _ownedCountText.text = string.Format(_ownedFormat, ownedCount);
            }
        }

        if (_icon != null)
        {
            _icon.sprite = entry.Icon;
            _icon.enabled = entry.Icon != null;
            _icon.color = level switch
            {
                RuneWordRevealLevel.Detailed when showFill => _onFillIconColor,
                RuneWordRevealLevel.Detailed when isElement => elementColor.Value,
                RuneWordRevealLevel.Detailed => _detailedIconColor,
                RuneWordRevealLevel.Listed => _listedIconColor,
                _ => _hiddenIconColor
            };
        }
    }

    private void SetHovered(bool isHovered)
    {
        _isHovered = isHovered;
        if (_hoverGlow != null)
        {
            _hoverGlow.SetActive(isHovered);
        }

        if (_hoverFrame != null)
        {
            _hoverFrame.SetActive(isHovered);
        }

        if (_frame != null)
        {
            _frame.SetActive(!isHovered || _hoverFrame == null);
        }
    }
}
