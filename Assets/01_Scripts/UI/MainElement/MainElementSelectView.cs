using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 원정 출발 전 메인 원소 선택 화면. 확정한 원소의 룬 효과를 기본 원소 슬롯에 장착한다.
///
/// 조작
/// - 원소 선택 중: 좌/우 방향키로 원을 따라 원소를 바꾸고, F를 누르거나 아래 방향키를 누르면 Confirm 버튼으로 내려간다.
/// - 버튼 선택 중: 좌/우 방향키로 Confirm / Cancel을 오가고, F로 누른다. 위 방향키를 누르면 원소 선택으로 돌아간다.
/// - Esc: Cancel
/// - 마우스로 원소 구슬과 버튼을 눌러도 된다.
/// </summary>
[DisallowMultipleComponent]
public class MainElementSelectView : MonoBehaviour
{
    [Serializable]
    private class ElementText
    {
        public ReactionElement element;
        public string effect;
    }

    [Header("원소 구슬 (원을 따라 시계 방향 순서)")]
    [SerializeField] private MainElementOrbView[] _orbs;

    [Header("가운데 정보")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _effectText;
    [SerializeField] private ElementText[] _effects =
    {
        new() { element = ReactionElement.Fire, effect = "Burn - damage over time" },
        new() { element = ReactionElement.Water, effect = "Slow - reduces move speed" },
        new() { element = ReactionElement.Earth, effect = "Stagger - interrupts actions" },
        new() { element = ReactionElement.Wind, effect = "Knockback - pushes enemies away" }
    };

    [Header("버튼")]
    [SerializeField] private MainElementButtonView _confirmButton;
    [SerializeField] private MainElementButtonView _cancelButton;

    [Header("키")]
    [SerializeField] private KeyCode _submitKey = KeyCode.F;
    [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;

    private int _selectedIndex;
    private bool _isButtonFocused;
    private bool _isConfirmFocused = true;

    /// <summary>Confirm을 눌렀을 때 (고른 원소)</summary>
    public event Action<ReactionElement> Confirmed;

    /// <summary>Cancel을 눌렀을 때</summary>
    public event Action Cancelled;

    public ReactionElement SelectedElement =>
        _orbs != null && _orbs.Length > 0 ? _orbs[_selectedIndex].Element : ReactionElement.Fire;

    private void Awake()
    {
        if (_orbs != null)
        {
            foreach (MainElementOrbView orb in _orbs)
            {
                if (orb != null)
                {
                    orb.Clicked += HandleOrbClicked;
                }
            }
        }

        if (_confirmButton != null && _confirmButton.Button != null)
        {
            _confirmButton.Button.onClick.AddListener(Confirm);
        }

        if (_cancelButton != null && _cancelButton.Button != null)
        {
            _cancelButton.Button.onClick.AddListener(Cancel);
        }
    }

    private void OnEnable()
    {
        _isButtonFocused = false;
        _isConfirmFocused = true;
        Refresh();
    }

    private void OnDestroy()
    {
        if (_orbs != null)
        {
            foreach (MainElementOrbView orb in _orbs)
            {
                if (orb != null)
                {
                    orb.Clicked -= HandleOrbClicked;
                }
            }
        }

        if (_confirmButton != null && _confirmButton.Button != null)
        {
            _confirmButton.Button.onClick.RemoveListener(Confirm);
        }

        if (_cancelButton != null && _cancelButton.Button != null)
        {
            _cancelButton.Button.onClick.RemoveListener(Cancel);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(_cancelKey))
        {
            Cancel();
            return;
        }

        if (!_isButtonFocused)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Select(_selectedIndex + 1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Select(_selectedIndex - 1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(_submitKey))
            {
                FocusButtons(true);
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            _isConfirmFocused = !_isConfirmFocused;
            Refresh();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            _isButtonFocused = false;
            Refresh();
        }
        else if (Input.GetKeyDown(_submitKey))
        {
            if (_isConfirmFocused)
            {
                Confirm();
            }
            else
            {
                Cancel();
            }
        }
    }

    public void Select(int index)
    {
        if (_orbs == null || _orbs.Length == 0)
        {
            return;
        }

        _selectedIndex = (index % _orbs.Length + _orbs.Length) % _orbs.Length;
        Refresh();
    }

    private void FocusButtons(bool confirm)
    {
        _isButtonFocused = true;
        _isConfirmFocused = confirm;
        Refresh();
    }

    private void HandleOrbClicked(MainElementOrbView orb)
    {
        int index = Array.IndexOf(_orbs, orb);
        if (index >= 0)
        {
            _isButtonFocused = false;
            Select(index);
        }
    }

    private void Confirm()
    {
        RuneEffectLoadoutManager manager = RuneEffectLoadoutManager.Instance;
        if (manager == null || !manager.Initialized)
        {
            throw new InvalidOperationException("RuneEffectLoadoutManager가 초기화되지 않았습니다.");
        }

        string effectId = SelectedElement switch
        {
            ReactionElement.Fire => "rune_fire",
            ReactionElement.Water => "rune_water",
            ReactionElement.Earth => "rune_earth",
            ReactionElement.Wind => "rune_wind",
            _ => throw new ArgumentOutOfRangeException(nameof(SelectedElement))
        };
        manager.RuneEffectLoadout.EquipPrimaryElement(effectId);
        Debug.Log($"[MainElementSelectView] Confirm: {SelectedElement}", this);
        Confirmed?.Invoke(SelectedElement);
    }

    private void Cancel()
    {
        Debug.Log("[MainElementSelectView] Cancel", this);
        Cancelled?.Invoke();
    }

    private void Refresh()
    {
        if (_orbs != null)
        {
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] != null)
                {
                    _orbs[i].SetSelected(i == _selectedIndex);
                }
            }
        }

        ReactionElement element = SelectedElement;
        if (_nameText != null)
        {
            _nameText.text = ReactionElementStyle.GetName(element);
        }

        if (_effectText != null)
        {
            _effectText.text = GetEffect(element);
        }

        if (_confirmButton != null)
        {
            _confirmButton.SetFocused(_isButtonFocused && _isConfirmFocused);
        }

        if (_cancelButton != null)
        {
            _cancelButton.SetFocused(_isButtonFocused && !_isConfirmFocused);
        }
    }

    private string GetEffect(ReactionElement element)
    {
        if (_effects != null)
        {
            foreach (ElementText text in _effects)
            {
                if (text != null && text.element == element)
                {
                    return text.effect;
                }
            }
        }

        return string.Empty;
    }
}
