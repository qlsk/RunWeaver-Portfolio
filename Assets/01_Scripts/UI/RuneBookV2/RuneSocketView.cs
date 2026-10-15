using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 새 룬북(RuneBookV2) Activated 띠의 장착 칸 하나.
/// - Equipped: 확정된 룬. 테두리 + 빛 + 채움 + 아이콘
/// - Pending: 확정 전에 넣은 룬. 주황 점선 고리 + 반투명 채움 + 아이콘
/// - Empty: 열린 빈 칸. "+" 표시
/// - Locked: 아직 열리지 않은 칸. 자물쇠 표시
/// 오른쪽 클릭하면 알린다. (프레젠터가 룬을 뺀다)
/// </summary>
[DisallowMultipleComponent]
public class RuneSocketView : MonoBehaviour, IPointerClickHandler
{
    public enum State
    {
        Empty,
        Equipped,
        Pending,
        Locked
    }

    [Tooltip("메인 원소 칸이면 체크 (테두리와 글자를 항상 금색으로)")]
    [SerializeField] private bool _isMain;

    [Header("구성 요소")]
    [SerializeField] private Image _rim;
    [SerializeField] private Image _innerRing;
    [SerializeField] private Image _glow;
    [SerializeField] private Image _fill;
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _pendingRing;
    [SerializeField] private GameObject _emptyMark;
    [SerializeField] private GameObject _lockMark;
    [SerializeField] private TMP_Text _label;

    [Header("테두리 색")]
    [SerializeField] private Color _mainRimColor = new(0.839f, 0.698f, 0.384f, 1f);
    [SerializeField] private Color _equippedRimColor = new(0.839f, 0.698f, 0.384f, 1f);
    [SerializeField] private Color _emptyRimColor = new(0.588f, 0.471f, 0.353f, 1f);
    [SerializeField] private Color _lockedRimColor = new(0.373f, 0.294f, 0.255f, 1f);
    [SerializeField] private Color _innerRingColor = new(0.471f, 0.353f, 0.275f, 1f);
    [SerializeField] private Color _lockedInnerRingColor = new(0.294f, 0.235f, 0.204f, 1f);

    [Header("빛 색")]
    [SerializeField] private Color _mainGlowColor = new(0.839f, 0.698f, 0.384f, 0.6f);
    [SerializeField] private Color _glowColor = new(0.902f, 0.784f, 0.588f, 0.6f);

    [Header("채움 색 (원소가 아닌 룬)")]
    [SerializeField] private Color _runeFillColor = new(0.839f, 0.769f, 0.659f, 1f);
    [SerializeField, Range(0f, 1f)] private float _pendingFillAlpha = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _pendingIconAlpha = 0.8f;

    [Header("아이콘 색")]
    [SerializeField] private Color _runeIconColor = new(0.227f, 0.141f, 0.094f, 1f);
    [SerializeField] private Color _elementIconColor = new(0.98f, 0.94f, 0.88f, 1f);

    [Header("글자 색")]
    [SerializeField] private Color _mainLabelColor = new(0.839f, 0.698f, 0.384f, 1f);
    [SerializeField] private Color _pendingLabelColor = new(0.941f, 0.745f, 0.431f, 1f);
    [SerializeField] private Color _labelColor = new(0.745f, 0.627f, 0.510f, 1f);

    public event Action<RuneSocketView> RightClicked;

    public int Index { get; set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RightClicked?.Invoke(this);
        }
    }

    /// <param name="icon">룬 효과 아이콘 (Equipped / Pending일 때)</param>
    /// <param name="elementColor">원소 룬이면 원소 색, 아니면 null</param>
    public void Show(State state, Sprite icon, Color? elementColor)
    {
        bool hasRune = state is State.Equipped or State.Pending;

        if (_rim != null)
        {
            _rim.gameObject.SetActive(state != State.Pending);
            _rim.color = state switch
            {
                State.Locked => _lockedRimColor,
                _ when _isMain => _mainRimColor,
                State.Equipped => _equippedRimColor,
                _ => _emptyRimColor
            };
        }

        if (_innerRing != null)
        {
            _innerRing.color = state == State.Locked ? _lockedInnerRingColor : _innerRingColor;
        }

        if (_glow != null)
        {
            _glow.gameObject.SetActive(state == State.Equipped);
            _glow.color = _isMain ? _mainGlowColor : _glowColor;
        }

        if (_pendingRing != null)
        {
            _pendingRing.SetActive(state == State.Pending);
        }

        if (_fill != null)
        {
            _fill.gameObject.SetActive(hasRune);
            Color fill = elementColor ?? _runeFillColor;
            fill.a = state == State.Pending ? _pendingFillAlpha : 1f;
            _fill.color = fill;
        }

        if (_icon != null)
        {
            _icon.gameObject.SetActive(hasRune && icon != null);
            _icon.sprite = icon;
            Color iconColor = elementColor.HasValue ? _elementIconColor : _runeIconColor;
            iconColor.a = state == State.Pending ? _pendingIconAlpha : 1f;
            _icon.color = iconColor;
        }

        if (_emptyMark != null)
        {
            _emptyMark.SetActive(state == State.Empty);
        }

        if (_lockMark != null)
        {
            _lockMark.SetActive(state == State.Locked);
        }

        if (_label != null)
        {
            _label.color = _isMain ? _mainLabelColor : state == State.Pending ? _pendingLabelColor : _labelColor;
        }
    }
}
