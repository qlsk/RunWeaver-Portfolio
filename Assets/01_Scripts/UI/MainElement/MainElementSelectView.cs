using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 원정 출발 전 메인 원소 선택 화면. 확정한 원소의 룬 효과를 기본 원소 슬롯에 장착한다.
///
/// 조작
/// - W/A/S/D(또는 방향키): 위(불), 왼쪽(물), 아래(흙), 오른쪽(바람)에 있는 원소 구슬을 바로 고른다.
/// - F 또는 Space: 고른 원소로 확정한다. (마우스가 Cancel 버튼 위에 있으면 취소)
/// - Esc: Cancel
/// - 마우스로 원소 구슬과 버튼을 눌러도 된다.
///
/// 연출 (실제 시간) : 프리팹 Animator(MainElementSelect.controller)의 클립이 한다. 연출할 때만 Animator를 켠다.
/// - 열기(Open) : 어둡게 깔리고, 링·구슬·제목·버튼이 아래에서 살짝 떠오르며 나타난다. 도중에 F/Space/클릭이면 바로 끝낸다.
/// - 확정(Confirm원소) : 고른 구슬이 커지며 가운데로 모이고 나머지는 가운데로 빨려 들며 사라진다.
///          가운데에서 원소 색 빛(Circle/Burst)이 터진 뒤 원소를 장착하고 Confirmed를 알린다.
/// - 취소(Cancel) : 아래로 살짝 가라앉으며 사라진 뒤 Cancelled를 알린다.
/// 연출이 끝나면 Animator를 끄고, 구슬 크기 같은 선택 표시는 Refresh가 다시 맞춘다.
/// </summary>
[DisallowMultipleComponent]
public class MainElementSelectView : MonoBehaviour
{
    [Serializable]
    private class ElementText
    {
        public ReactionElement Element;
        public string Effect;
    }

    [Header("원소 구슬 (원을 따라 시계 방향 순서)")]
    [SerializeField] private MainElementOrbView[] _orbs;

    [Header("가운데 정보")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _effectText;
    [SerializeField] private ElementText[] _effects =
    {
        new() { Element = ReactionElement.Fire, Effect = "화상 - 지속 피해" },
        new() { Element = ReactionElement.Water, Effect = "감속 - 이동 속도 감소" },
        new() { Element = ReactionElement.Earth, Effect = "경직 - 행동 중단" },
        new() { Element = ReactionElement.Wind, Effect = "넉백 - 적을 밀쳐냄" }
    };

    [Header("버튼")]
    [SerializeField] private MainElementButtonView _confirmButton;
    [SerializeField] private MainElementButtonView _cancelButton;

    [Header("키")]
    [SerializeField] private KeyCode _submitKey = KeyCode.F;
    [SerializeField] private KeyCode _altSubmitKey = KeyCode.Space;
    [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;

    [Header("연출")]
    [Tooltip("열기, 확정, 취소 클립을 재생하는 Animator (연출할 때만 켠다)")]
    [SerializeField] private Animator _animator;

    private int _selectedIndex;
    private bool _isButtonFocused;
    private bool _isMouseFocused;
    private bool _isConfirmFocused = true;

    private enum Phase
    {
        Idle,
        Opening,
        Closing
    }

    private Phase _phase;
    private bool _skipOpening;
    private MainElementButtonView _hoveredButton;
    private MainElementOrbView _hoveredOrb;

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
                    orb.PointerEntered += HandleOrbPointerEntered;
                    orb.PointerExited += HandleOrbPointerExited;
                }
            }
        }

        if (_confirmButton != null && _confirmButton.Button != null)
        {
            _confirmButton.Button.onClick.AddListener(Confirm);
            _confirmButton.PointerEntered += HandleConfirmHovered;
            _confirmButton.PointerExited += HandleConfirmUnhovered;
        }

        if (_cancelButton != null && _cancelButton.Button != null)
        {
            _cancelButton.Button.onClick.AddListener(Cancel);
            _cancelButton.PointerEntered += HandleCancelHovered;
            _cancelButton.PointerExited += HandleCancelUnhovered;
        }
    }

    private void OnEnable()
    {
        _isButtonFocused = false;
        _isMouseFocused = false;
        _isConfirmFocused = true;
        Refresh();
        StartCoroutine(OpenRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        // 다시 켜질 때 Open 클립이 처음부터 모든 모습을 다시 맞춘다.
        if (_animator != null)
        {
            _animator.enabled = false;
        }

        _phase = Phase.Idle;

        // 비활성화되면 PointerExit이 오지 않으므로 기록해 둔 포인터 대상을 비운다.
        _hoveredButton = null;
        _hoveredOrb = null;
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
                    orb.PointerEntered -= HandleOrbPointerEntered;
                    orb.PointerExited -= HandleOrbPointerExited;
                }
            }
        }

        if (_confirmButton != null && _confirmButton.Button != null)
        {
            _confirmButton.Button.onClick.RemoveListener(Confirm);
            _confirmButton.PointerEntered -= HandleConfirmHovered;
            _confirmButton.PointerExited -= HandleConfirmUnhovered;
        }

        if (_cancelButton != null && _cancelButton.Button != null)
        {
            _cancelButton.Button.onClick.RemoveListener(Cancel);
            _cancelButton.PointerEntered -= HandleCancelHovered;
            _cancelButton.PointerExited -= HandleCancelUnhovered;
        }
    }

    private void Update()
    {
        if (_phase == Phase.Opening)
        {
            if (IsSubmitPressed() || Input.GetMouseButtonDown(0))
            {
                _skipOpening = true;
            }

            return;
        }

        if (_phase == Phase.Closing)
        {
            return;
        }

        if (Input.GetKeyDown(_cancelKey))
        {
            Cancel();
            return;
        }

        // W/A/S/D(또는 방향키) : 그 방향에 있는 원소 구슬을 바로 고른다.
        if (TryGetDirectionPressed(out Vector2 direction))
        {
            SelectByDirection(direction);
            return;
        }

        // F/Space : 확정. 마우스가 Cancel 버튼 위에 있으면 취소한다.
        if (IsSubmitPressed())
        {
            if (_isButtonFocused && !_isConfirmFocused)
            {
                Cancel();
            }
            else
            {
                Confirm();
            }
        }
    }

    private bool IsSubmitPressed() => Input.GetKeyDown(_submitKey) || Input.GetKeyDown(_altSubmitKey);

    // 방향 입력 : WASD와 방향키를 같이 받는다.
    private static bool TryGetDirectionPressed(out Vector2 direction)
    {
        direction = Vector2.zero;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) direction = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) direction = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) direction = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) direction = Vector2.right;
        return direction != Vector2.zero;
    }

    /// <summary>원의 가운데에서 보아 direction 쪽에 가장 가까이 놓인 구슬을 고른다. (위 불, 오른쪽 바람, 아래 흙, 왼쪽 물)</summary>
    private void SelectByDirection(Vector2 direction)
    {
        if (_orbs == null || _orbs.Length == 0)
        {
            return;
        }

        int best = -1;
        float bestDot = float.NegativeInfinity;
        for (int i = 0; i < _orbs.Length; i++)
        {
            if (_orbs[i] == null)
            {
                continue;
            }

            Vector2 position = ((RectTransform)_orbs[i].transform).anchoredPosition;
            if (position.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            float dot = Vector2.Dot(position.normalized, direction);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = i;
            }
        }

        // 잠긴 구슬이 가리킨 방향이면 다른 구슬로 옮기지 않고 현재 선택을 유지한다.
        if (best < 0 || _orbs[best].IsLocked)
        {
            return;
        }

        _isButtonFocused = false;
        _isMouseFocused = false;
        Select(best);
    }

    private void HandleConfirmHovered() => HandleButtonHovered(_confirmButton);
    private void HandleCancelHovered() => HandleButtonHovered(_cancelButton);

    // 마우스가 올라온 대상은 연출 중에도 기록해 두고, 연출이 끝나면 ApplyPointerState로 반영한다.
    private void HandleButtonHovered(MainElementButtonView button)
    {
        _hoveredButton = button;
        if (_phase != Phase.Idle)
            return;

        FocusButtons(button == _confirmButton, true);
    }

    private void HandleConfirmUnhovered() => HandleButtonUnhovered(_confirmButton);
    private void HandleCancelUnhovered() => HandleButtonUnhovered(_cancelButton);

    // 마우스가 벗어난 버튼이 지금 포커스된 버튼일 때만 해제한다. (키보드로 다른 버튼에 옮겨 간 경우는 건드리지 않는다)
    // 구슬 연출을 건드리지 않도록 전체 Refresh 대신 버튼 표시만 갱신한다.
    private void HandleButtonUnhovered(MainElementButtonView button)
    {
        if (_hoveredButton == button)
            _hoveredButton = null;

        bool confirm = button == _confirmButton;
        if (!_isMouseFocused || !_isButtonFocused || _isConfirmFocused != confirm)
            return;

        _isButtonFocused = false;
        _isMouseFocused = false;
        RefreshButtons();
    }

    /// <summary>연출 중 올라온 마우스가 지금도 대상 위에 있으면 호버 표시와 버튼 포커스를 적용한다.</summary>
    private void ApplyPointerState()
    {
        if (_hoveredButton != null)
        {
            FocusButtons(_hoveredButton == _confirmButton, true);
        }
        else if (_hoveredOrb != null)
        {
            _hoveredOrb.SetHovered(true);
        }
    }

    /// <summary>선택할 수 없게 막을 원소. null 또는 빈 목록이면 모두 연다. (튜토리얼)</summary>
    public void SetLockedElements(IReadOnlyCollection<ReactionElement> lockedElements)
    {
        if (_orbs == null)
        {
            return;
        }

        foreach (MainElementOrbView orb in _orbs)
        {
            if (orb != null)
            {
                orb.SetLocked(IsLockedElement(lockedElements, orb.Element));
            }
        }

        if (_orbs[_selectedIndex] != null && _orbs[_selectedIndex].IsLocked)
        {
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] != null && !_orbs[i].IsLocked)
                {
                    _selectedIndex = i;
                    break;
                }
            }
        }

        Refresh();
    }

    private static bool IsLockedElement(IReadOnlyCollection<ReactionElement> lockedElements, ReactionElement element)
    {
        if (lockedElements == null)
        {
            return false;
        }

        foreach (ReactionElement locked in lockedElements)
        {
            if (locked == element)
            {
                return true;
            }
        }

        return false;
    }

    public void Select(int index)
    {
        if (_orbs == null || _orbs.Length == 0)
        {
            return;
        }

        int targetIndex = (index % _orbs.Length + _orbs.Length) % _orbs.Length;
        if (_orbs[targetIndex] != null && _orbs[targetIndex].IsLocked)
        {
            return;
        }

        _selectedIndex = targetIndex;
        Refresh();
        if (_phase == Phase.Idle && _orbs[_selectedIndex] != null)
        {
            Sfx.Play(_orbs[_selectedIndex].Element switch
            {
                ReactionElement.Fire => Sfx.ElementFire,
                ReactionElement.Water => Sfx.ElementWater,
                ReactionElement.Wind => Sfx.ElementWind,
                _ => Sfx.ElementEarth,
            });
        }
    }

    private void FocusButtons(bool confirm, bool byMouse = false)
    {
        _isButtonFocused = true;
        _isMouseFocused = byMouse;
        _isConfirmFocused = confirm;
        Refresh();
    }

    private void HandleOrbClicked(MainElementOrbView orb)
    {
        if (_phase != Phase.Idle)
        {
            return;
        }

        int index = Array.IndexOf(_orbs, orb);
        if (index >= 0)
        {
            _isButtonFocused = false;
            _isMouseFocused = false;
            Select(index);
        }
    }

    // 호버는 표시만 바꾸고 선택(_selectedIndex)은 건드리지 않는다.
    private void HandleOrbPointerEntered(MainElementOrbView orb)
    {
        _hoveredOrb = orb;
        if (_phase != Phase.Idle)
        {
            return;
        }

        orb.SetHovered(true);
    }

    private void HandleOrbPointerExited(MainElementOrbView orb)
    {
        if (_hoveredOrb == orb)
            _hoveredOrb = null;

        orb.SetHovered(false);
    }

    private void ClearOrbHover()
    {
        if (_orbs == null)
        {
            return;
        }

        foreach (MainElementOrbView orb in _orbs)
        {
            if (orb != null)
            {
                orb.SetHovered(false);
            }
        }
    }

    private void Confirm()
    {
        if (_phase != Phase.Idle)
        {
            return;
        }

        ClearOrbHover();
        StartCoroutine(ConfirmRoutine());
    }

    private void EquipAndNotify()
    {
        PlayerRuneManager manager = PlayerRuneManager.Instance;
        if (manager == null || !manager.IsInitialized)
        {
            throw new InvalidOperationException("PlayerRuneManager가 초기화되지 않았습니다.");
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
        if (_phase != Phase.Idle)
        {
            return;
        }

        ClearOrbHover();
        StartCoroutine(CancelRoutine());
    }

    // ───────────────────────── 연출 ─────────────────────────

    /// <summary>클립을 처음부터 재생하고 끝날 때까지(실제 시간) 기다린다. skip이 켜지면 바로 끝 프레임으로 넘긴다.</summary>
    private IEnumerator PlayClip(string state, Func<bool> skip = null)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            yield break;
        }

        _animator.enabled = true;
        _animator.Play(state, 0, 0f);
        _animator.Update(0f);
        float length = _animator.GetCurrentAnimatorStateInfo(0).length;
        float elapsed = 0f;
        while (elapsed < length && (skip == null || !skip()))
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _animator.Play(state, 0, 1f);
        _animator.Update(0f);
    }

    /// <summary>열린 모습(Open의 끝 프레임)으로 맞추고 Animator를 끈다. 선택 표시는 Refresh가 다시 맞춘다.</summary>
    private void StopAnimation()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            return;
        }

        _animator.enabled = true;
        _animator.Play("Open", 0, 1f);
        _animator.Update(0f);
        _animator.enabled = false;
        Refresh();
    }

    private IEnumerator OpenRoutine()
    {
        _phase = Phase.Opening;
        _skipOpening = false;
        yield return PlayClip("Open", () => _skipOpening);
        StopAnimation();
        _phase = Phase.Idle;
        ApplyPointerState();
    }

    private IEnumerator CancelRoutine()
    {
        _phase = Phase.Closing;
        yield return PlayClip("Cancel");
        Debug.Log("[MainElementSelectView] Cancel", this);
        Cancelled?.Invoke();
        FinishIfStillOpen();
    }

    /// <summary>알림 뒤에도 창이 켜져 있으면(받는 쪽이 닫지 않으면) 원래 모습으로 되돌린다.</summary>
    private void FinishIfStillOpen()
    {
        if (isActiveAndEnabled)
        {
            StopAnimation();
            _phase = Phase.Idle;
            ApplyPointerState();
        }
    }

    private IEnumerator ConfirmRoutine()
    {
        _phase = Phase.Closing;
        yield return PlayClip("Confirm" + SelectedElement);
        EquipAndNotify();
        FinishIfStillOpen();
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

        RefreshButtons();
    }

    /// <summary>버튼 포커스 표시만 갱신한다. (구슬 크기 같은 연출 상태는 건드리지 않는다)</summary>
    private void RefreshButtons()
    {
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
                if (text != null && text.Element == element)
                {
                    return text.Effect;
                }
            }
        }

        return string.Empty;
    }
}