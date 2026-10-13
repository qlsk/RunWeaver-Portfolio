using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 스킬 칸에 대시, 쉴드(Barrier), 정화(Cleanse)의 쿨타임을 표시한다.
/// - 쿨타임 동안 아이콘이 어두워지고 시계 방향으로 다시 채워진다. 뒤에는 희미한 원래 아이콘(Ghost)이 깔린다.
/// - 남은 시간을 칸 가운데에 초 단위로 보여준다. (1초 이상은 정수, 1초 미만은 소수 한 자리)
/// - 다시 쓸 수 있게 되면 아이콘이 살짝 튀어 오른다.
/// - Barrier : 쉴드 스택 수를 숫자로 보여주고, 다음 스택이 생길 때까지 아이콘이 채워진다. 스택이 0이면 어둡게 표시한다.
/// 플레이어(Dash, Shield, Cleanse)는 씬에서 찾는다. 없는 씬에서는 아이콘을 쓸 수 있는 상태로 둔다.
/// </summary>
[DisallowMultipleComponent]
public class SkillSlotsHud : MonoBehaviour
{
    private const float SearchInterval = 1f;

    [Header("Dash")]
    [SerializeField] private Image _dashIcon;
    [Tooltip("쿨타임 중 뒤에 희미하게 보이는 원래 아이콘")]
    [SerializeField] private Image _dashGhost;
    [Tooltip("남은 쿨타임(초)")]
    [SerializeField] private TMP_Text _dashTimeText;

    [Header("Barrier (Shield)")]
    [SerializeField] private Image _barrierIcon;
    [SerializeField] private Image _barrierGhost;
    [Tooltip("다음 쉴드가 생길 때까지 남은 시간(초)")]
    [SerializeField] private TMP_Text _barrierTimeText;
    [Tooltip("쉴드 스택 수. 비워두면 숫자를 표시하지 않는다")]
    [SerializeField] private TMP_Text _barrierStackText;

    [Header("Cleanse")]
    [SerializeField] private Image _cleanseIcon;
    [SerializeField] private Image _cleanseGhost;
    [SerializeField] private TMP_Text _cleanseTimeText;

    [Header("색")]
    [SerializeField] private Color _readyColor = Color.white;
    [SerializeField] private Color _coolingColor = new(0.4f, 0.4f, 0.4f, 1f);

    [Header("다시 쓸 수 있을 때")]
    [Tooltip("아이콘이 튀어 오르는 크기")]
    [SerializeField, Min(1f)] private float _readyPunchScale = 1.15f;
    [SerializeField, Min(0.01f)] private float _readyPunchDuration = 0.2f;

    private sealed class SlotState
    {
        public bool HasState;
        public bool WasReady;
        public int LastStacks;
        public float PunchTime = -1f;
    }

    private readonly SlotState _dashState = new();
    private readonly SlotState _barrierState = new();
    private readonly SlotState _cleanseState = new();

    private Dash _dash;
    private Shield _shield;
    private Cleanse _cleanse;
    private float _nextSearchTime;

    private void Awake()
    {
        SetupFill(_dashIcon);
        SetupFill(_barrierIcon);
        SetupFill(_cleanseIcon);
    }

    private void LateUpdate()
    {
        FindPlayerSkills();
        RefreshDash();
        RefreshBarrier();
        RefreshCleanse();
    }

    private void FindPlayerSkills()
    {
        if ((_dash != null && _shield != null && _cleanse != null) || Time.unscaledTime < _nextSearchTime)
        {
            return;
        }

        _nextSearchTime = Time.unscaledTime + SearchInterval;
        if (_dash == null)
        {
            _dash = FindAnyObjectByType<Dash>();
        }

        if (_shield == null)
        {
            _shield = FindAnyObjectByType<Shield>();
        }

        if (_cleanse == null)
        {
            _cleanse = FindAnyObjectByType<Cleanse>();
        }
    }

    private void RefreshDash()
    {
        float remaining = 0f;
        float progress = 1f;
        if (_dash != null && _dash.CooldownDuration > 0f)
        {
            remaining = _dash.CooldownRemaining;
            progress = 1f - remaining / _dash.CooldownDuration;
        }

        bool ready = progress >= 1f;
        Refresh(_dashIcon, _dashGhost, _dashTimeText, _dashState, progress, ready, remaining, ready);
    }

    private void RefreshBarrier()
    {
        if (_shield == null)
        {
            Refresh(_barrierIcon, _barrierGhost, _barrierTimeText, _barrierState, 1f, true, 0f, false);
            SetText(_barrierStackText, string.Empty);
            return;
        }

        int stacks = _shield.CurrentShield;
        bool full = stacks >= _shield.MaxShield;
        float progress = full ? 1f : _shield.RechargeProgress;
        float remaining = _shield.IsRecharging ? _shield.RechargeRemaining : 0f;

        // 스택이 늘어나면 튀어 오른다.
        bool gained = _barrierState.HasState && stacks > _barrierState.LastStacks;
        _barrierState.LastStacks = stacks;

        Refresh(_barrierIcon, _barrierGhost, _barrierTimeText, _barrierState, progress, stacks > 0, remaining, gained);
        SetText(_barrierStackText, stacks > 0 ? stacks.ToString() : string.Empty);
    }

    private void RefreshCleanse()
    {
        float remaining = 0f;
        float progress = 1f;
        if (_cleanse != null && _cleanse.CooldownDuration > 0f)
        {
            remaining = _cleanse.CooldownRemaining;
            progress = 1f - remaining / _cleanse.CooldownDuration;
        }

        bool ready = progress >= 1f;
        Refresh(_cleanseIcon, _cleanseGhost, _cleanseTimeText, _cleanseState, progress, ready, remaining, ready);
    }

    /// <summary>
    /// 칸 하나를 갱신한다.
    /// punchWhenReady : 이번에 "쓸 수 있게 됨"으로 바뀌었는지 판단할 값 (true로 바뀌는 순간 튀어 오른다)
    /// </summary>
    private void Refresh(Image icon, Image ghost, TMP_Text timeText, SlotState state, float fillAmount, bool isReady,
        float remaining, bool punchWhenReady)
    {
        fillAmount = Mathf.Clamp01(fillAmount);

        if (icon != null)
        {
            icon.fillAmount = fillAmount;
            icon.color = isReady ? _readyColor : _coolingColor;
        }

        // 아이콘이 다 차 있지 않거나 쓸 수 없을 때만 희미한 원래 아이콘을 깐다.
        if (ghost != null)
        {
            bool showGhost = fillAmount < 1f || !isReady;
            if (ghost.enabled != showGhost)
            {
                ghost.enabled = showGhost;
            }
        }

        SetText(timeText, remaining > 0f ? FormatSeconds(remaining) : string.Empty);

        if (state.HasState && punchWhenReady && !state.WasReady)
        {
            state.PunchTime = 0f;
        }

        state.WasReady = punchWhenReady;
        state.HasState = true;
        UpdatePunch(icon, state);
    }

    private void UpdatePunch(Image icon, SlotState state)
    {
        if (icon == null || state.PunchTime < 0f)
        {
            return;
        }

        state.PunchTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(state.PunchTime / _readyPunchDuration);
        float scale = Mathf.Lerp(_readyPunchScale, 1f, 1f - (1f - t) * (1f - t));
        icon.rectTransform.localScale = Vector3.one * scale;
        if (t >= 1f)
        {
            state.PunchTime = -1f;
            icon.rectTransform.localScale = Vector3.one;
        }
    }

    private static string FormatSeconds(float seconds)
    {
        return seconds >= 1f
            ? Mathf.CeilToInt(seconds).ToString()
            : (Mathf.Ceil(seconds * 10f) / 10f).ToString("0.0");
    }

    private static void SetText(TMP_Text label, string text)
    {
        if (label != null && label.text != text)
        {
            label.text = text;
        }
    }

    private static void SetupFill(Image icon)
    {
        if (icon == null)
        {
            return;
        }

        icon.type = Image.Type.Filled;
        icon.fillMethod = Image.FillMethod.Radial360;
        icon.fillOrigin = (int)Image.Origin360.Top;
        icon.fillClockwise = true;
    }
}
