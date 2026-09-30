using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룬 문자 칸 하나의 표시를 담당한다. 룬북 목록 칸, 활성 룬 칸, 상세 아이콘에서 함께 사용한다.
/// </summary>
[DisallowMultipleComponent]
public class RuneSymbolSlotView : MonoBehaviour
{
    [Header("구성 요소")]
    [SerializeField] private Image _frameImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _button;

    [Header("아이콘 색상")]
    [SerializeField] private Color _lockedIconColor = new(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color _listedIconColor = new(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color _detailedIconColor = Color.white;
    [SerializeField] private Color _emptyIconColor = new(1f, 1f, 1f, 0f);

    [Header("테두리 색상")]
    [SerializeField] private Color _normalFrameColor = Color.white;
    [SerializeField] private Color _selectedFrameColor = new(1f, 0.78f, 0.25f, 1f);
    [SerializeField] private Color _lockedFrameColor = new(0.4f, 0.4f, 0.4f, 1f);

    private Sprite _defaultIcon;
    private RuneSymbolSlotState _state = RuneSymbolSlotState.Empty;
    private bool _isSelected;
    private bool _isClickable = true;
    private bool _isInitialized;

    public int Index { get; private set; }
    public event Action<RuneSymbolSlotView> Clicked;

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    public void SetIndex(int index)
    {
        Index = index;
    }

    /// <summary>
    /// 칸의 상태와 아이콘을 갱신한다. icon이 null이면 프리팹에 지정된 기본 이미지를 사용한다.
    /// </summary>
    public void Show(RuneSymbolSlotState state, Sprite icon)
    {
        Initialize();
        _state = state;

        if (_iconImage != null)
        {
            _iconImage.sprite = icon != null ? icon : _defaultIcon;
            _iconImage.color = GetIconColor(state);
        }

        ApplyInteractable();
        ApplyFrameColor();
    }

    public void SetSelected(bool isSelected)
    {
        Initialize();
        _isSelected = isSelected;
        ApplyFrameColor();
    }

    /// <summary>
    /// 상세 아이콘처럼 클릭이 필요 없는 칸에서 클릭을 막는다.
    /// </summary>
    public void SetClickable(bool isClickable)
    {
        Initialize();
        _isClickable = isClickable;
        ApplyInteractable();
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        if (_iconImage != null)
        {
            _defaultIcon = _iconImage.sprite;
        }

        if (_button != null)
        {
            _button.onClick.AddListener(HandleButtonClicked);
        }
        else
        {
            Debug.LogWarning($"[RuneSymbolSlotView] {name}: Button이 할당되지 않아 클릭할 수 없습니다.", this);
        }
    }

    private Color GetIconColor(RuneSymbolSlotState state)
    {
        return state switch
        {
            RuneSymbolSlotState.Locked => _lockedIconColor,
            RuneSymbolSlotState.Empty => _emptyIconColor,
            RuneSymbolSlotState.Listed => _listedIconColor,
            _ => _detailedIconColor
        };
    }

    private void ApplyInteractable()
    {
        if (_button == null)
        {
            return;
        }

        // 클릭이 필요 없는 칸은 Button 자체를 꺼서 비활성 색상 전환이 적용되지 않게 한다.
        _button.enabled = _isClickable;
        _button.interactable = _state != RuneSymbolSlotState.Locked;
    }

    private void ApplyFrameColor()
    {
        if (_frameImage == null)
        {
            return;
        }

        if (_state == RuneSymbolSlotState.Locked)
        {
            _frameImage.color = _lockedFrameColor;
            return;
        }

        _frameImage.color = _isSelected ? _selectedFrameColor : _normalFrameColor;
    }

    private void HandleButtonClicked()
    {
        Clicked?.Invoke(this);
    }
}
