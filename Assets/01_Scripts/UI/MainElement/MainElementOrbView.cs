using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 메인 원소 선택 화면의 원소 구슬 하나. 원소 색 바탕 + 흰 아이콘 + 이름표.
/// 선택되면 크게 키우고 밝은 테두리와 원소 색 빛을 켠다. (이름표는 가운데 정보로 대신하므로 숨긴다)
/// 마우스를 올리면(호버) 선택은 바뀌지 않고 회색 반투명 테두리만 보여준다.
/// </summary>
[DisallowMultipleComponent]
public class MainElementOrbView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const float SelectedGlowAlpha = 0.9f;
    private const float LockedDimRatio = 0.7f;

    [SerializeField] private ReactionElement _element;

    [Header("구성 요소")]
    [SerializeField] private Image _fill;
    [SerializeField] private Image _glow;
    [SerializeField] private GameObject _normalRing;
    [SerializeField] private GameObject _selectedRing;
    [Tooltip("선택되지 않은 구슬에 마우스를 올렸을 때 보이는 회색 테두리")]
    [SerializeField] private GameObject _hoverRing;
    [SerializeField] private TMP_Text _label;

    [Header("선택 표시")]
    [SerializeField] private float _selectedScale = 1.25f;

    private bool _isSelected;
    private bool _isHovered;

    /// <summary>true면 구슬이 어둡게 보이고 선택할 수 없다.</summary>
    public bool IsLocked { get; private set; }

    public event Action<MainElementOrbView> Clicked;
    public event Action<MainElementOrbView> PointerEntered;
    public event Action<MainElementOrbView> PointerExited;

    public ReactionElement Element => _element;

    /// <summary>선택 시 켜지는 원소 색 빛 (확정 연출이 복제해서 쓴다)</summary>
    public Image Glow => _glow;

    private void Awake()
    {
        Color color = ReactionElementStyle.GetColor(_element);
        if (_glow != null)
        {
            _glow.color = new Color(color.r, color.g, color.b, SelectedGlowAlpha);
        }

        RefreshFillColor();
    }

    /// <summary>잠그면 구슬이 어두워지고 호버 테두리가 뜨지 않는다. (튜토리얼에서 선택을 막을 때)</summary>
    public void SetLocked(bool isLocked)
    {
        IsLocked = isLocked;
        RefreshFillColor();
        RefreshHoverRing();
    }

    private void RefreshFillColor()
    {
        if (_fill == null)
        {
            return;
        }

        Color color = ReactionElementStyle.GetColor(_element);
        _fill.color = IsLocked ? Color.Lerp(color, Color.black, LockedDimRatio) : color;
    }

    private void OnDisable()
    {
        // 비활성화되면 PointerExit이 오지 않으므로 호버 상태를 직접 지운다.
        _isHovered = false;
        SetActive(_hoverRing, false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked?.Invoke(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExited?.Invoke(this);
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        transform.localScale = Vector3.one * (isSelected ? _selectedScale : 1f);
        SetActive(_glow != null ? _glow.gameObject : null, isSelected);
        SetActive(_selectedRing, isSelected);
        SetActive(_normalRing, !isSelected);
        SetActive(_label != null ? _label.gameObject : null, !isSelected);
        RefreshHoverRing();
    }

    /// <summary>마우스를 올린 표시. 선택 상태는 바꾸지 않는다.</summary>
    public void SetHovered(bool isHovered)
    {
        if (_isHovered == isHovered)
        {
            return;
        }

        _isHovered = isHovered;
        RefreshHoverRing();
    }

    // 선택된 구슬은 선택 테두리가 대신하므로, 선택되지 않은 구슬에 마우스를 올렸을 때만 보인다.
    private void RefreshHoverRing()
    {
        SetActive(_hoverRing, _isHovered && !_isSelected && !IsLocked);
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
