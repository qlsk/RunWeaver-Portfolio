using System.Collections.Generic;
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
/// 보상 특성 연출 (그림은 프리팹의 자식 이미지) :
/// - clease_04 : 다음 정화 대기시간이 줄면 별빛이 정화 칸 둘레를 한 바퀴 돌고 "-25%"가 떠오른다.
/// - shield_01 : 최대 보호막이 늘면 보호막 칸 뒤 고리가 몇 번 빛나고 "최대 3 > 4"가 뜬다.
/// 특성 연출은 게임 시간으로 흐른다. (보상 선택으로 멈춰 있는 동안은 기다렸다가 재생한다)
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

    [Header("특성 연출 : 정화 대기시간 감산 (clease_04)")]
    [Tooltip("칸 둘레를 도는 별. 자식 이미지(빛, 별)를 함께 움직인다")]
    [SerializeField] private RectTransform _cleanseRefundOrbiter;
    [Tooltip("별이 지나간 자리에 남는 빛 알갱이 견본 (꺼 둔 자식)")]
    [SerializeField] private Image _cleanseRefundTrail;
    [Tooltip("감산 비율 글자. 비율로 다시 쓴다 (예: -25%)")]
    [SerializeField] private TMP_Text _cleanseRefundText;
    [Tooltip("칸 중심에서 별까지의 거리 (픽셀)")]
    [SerializeField, Min(0f)] private float _refundOrbitRadius = 46f;
    [Tooltip("별이 출발하는 각도 (도, 위가 90)")]
    [SerializeField] private float _refundStartAngle = 90f;
    [Tooltip("도는 양 (도). 음수면 시계 방향")]
    [SerializeField] private float _refundSweep = -360f;
    [SerializeField, Min(0.01f)] private float _refundDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float _refundTrailInterval = 0.03f;
    [SerializeField, Min(0.01f)] private float _refundTrailLife = 0.35f;
    [SerializeField, Min(0.01f)] private float _refundTextDuration = 1.3f;
    [Tooltip("글자가 떠오르는 높이 (픽셀)")]
    [SerializeField] private float _refundTextRise = 14f;

    [Header("특성 연출 : 최대 보호막 증가 (shield_01)")]
    [Tooltip("보호막 칸 뒤에서 빛나는 고리")]
    [SerializeField] private Image _barrierMaxUpGlow;
    [Tooltip("최대치 글자. 바뀐 값으로 다시 쓴다 (예: 최대 3 > 4)")]
    [SerializeField] private TMP_Text _barrierMaxUpText;
    [SerializeField, Min(0.01f)] private float _maxUpDuration = 1.6f;
    [Tooltip("고리가 빛나는 횟수")]
    [SerializeField, Min(1)] private int _maxUpPulses = 3;
    [Tooltip("고리가 처음 커져 있는 배율 (1까지 줄어든다)")]
    [SerializeField, Min(1f)] private float _maxUpStartScale = 1.4f;

    private sealed class TrailMote
    {
        public Image Image;
        public float Age;
    }

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

    private readonly List<TrailMote> _refundTrails = new();
    private Image[] _refundOrbiterImages;
    private Color[] _refundOrbiterColors;
    private Color _refundTrailColor = Color.white;
    private Color _refundTextColor = Color.white;
    private Vector2 _refundTextPosition;
    private float _refundTime = -1f;
    private float _refundTrailTimer;

    private Color _maxUpGlowColor = Color.white;
    private Color _maxUpTextColor = Color.white;
    private float _maxUpTime = -1f;
    private int _lastMaxShield = -1;

    private void Awake()
    {
        SetupFill(_dashIcon);
        SetupFill(_barrierIcon);
        SetupFill(_cleanseIcon);
        SetupTraitFx();
    }

    private void OnDestroy()
    {
        if (_cleanse != null)
        {
            _cleanse.CooldownRefunded -= HandleCleanseRefunded;
        }
    }

    private void LateUpdate()
    {
        FindPlayerSkills();
        RefreshDash();
        RefreshBarrier();
        RefreshCleanse();
        UpdateRefundFx(Time.deltaTime);
        UpdateMaxUpFx(Time.deltaTime);
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
            if (_cleanse != null)
            {
                _cleanse.CooldownRefunded += HandleCleanseRefunded;
            }
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

        // 최대 보호막이 늘면 (shield_01) 칸 뒤 고리를 빛낸다.
        int maxShield = _shield.MaxShield;
        if (_lastMaxShield >= 0 && maxShield > _lastMaxShield)
        {
            PlayMaxUp(_lastMaxShield, maxShield);
        }

        _lastMaxShield = maxShield;
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

    private void SetupTraitFx()
    {
        if (_cleanseRefundOrbiter != null)
        {
            _refundOrbiterImages = _cleanseRefundOrbiter.GetComponentsInChildren<Image>(true);
            _refundOrbiterColors = new Color[_refundOrbiterImages.Length];
            for (int i = 0; i < _refundOrbiterImages.Length; i++)
            {
                _refundOrbiterColors[i] = _refundOrbiterImages[i].color;
            }

            _cleanseRefundOrbiter.gameObject.SetActive(false);
        }

        if (_cleanseRefundTrail != null)
        {
            _refundTrailColor = _cleanseRefundTrail.color;
            _cleanseRefundTrail.gameObject.SetActive(false);
        }

        if (_cleanseRefundText != null)
        {
            _refundTextColor = _cleanseRefundText.color;
            _refundTextPosition = _cleanseRefundText.rectTransform.anchoredPosition;
            _cleanseRefundText.gameObject.SetActive(false);
        }

        if (_barrierMaxUpGlow != null)
        {
            _maxUpGlowColor = _barrierMaxUpGlow.color;
            _barrierMaxUpGlow.gameObject.SetActive(false);
        }

        if (_barrierMaxUpText != null)
        {
            _maxUpTextColor = _barrierMaxUpText.color;
            _barrierMaxUpText.gameObject.SetActive(false);
        }
    }

    private void HandleCleanseRefunded(float ratio)
    {
        if (_cleanseRefundOrbiter == null && _cleanseRefundText == null)
        {
            return;
        }

        _refundTime = 0f;
        _refundTrailTimer = 0f;
        if (_cleanseRefundOrbiter != null)
        {
            _cleanseRefundOrbiter.gameObject.SetActive(true);
        }

        if (_cleanseRefundText != null)
        {
            _cleanseRefundText.text = $"-{Mathf.RoundToInt(ratio * 100f)}%";
            _cleanseRefundText.gameObject.SetActive(true);
        }

        UpdateRefundFx(0f);
    }

    private void UpdateRefundFx(float deltaTime)
    {
        UpdateRefundTrails(deltaTime);
        if (_refundTime < 0f)
        {
            return;
        }

        _refundTime += deltaTime;
        float t = Mathf.Clamp01(_refundTime / _refundDuration);
        if (_cleanseRefundOrbiter != null && _cleanseRefundOrbiter.gameObject.activeSelf)
        {
            if (t >= 1f)
            {
                _cleanseRefundOrbiter.gameObject.SetActive(false);
            }
            else
            {
                float eased = t * t * (3f - 2f * t);
                float radians = (_refundStartAngle + _refundSweep * eased) * Mathf.Deg2Rad;
                Vector2 position = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * _refundOrbitRadius;
                _cleanseRefundOrbiter.anchoredPosition = position;
                _cleanseRefundOrbiter.localEulerAngles = new Vector3(0f, 0f, _refundSweep * eased * 0.5f);

                float alpha = Mathf.Clamp01(t / 0.1f) * Mathf.Clamp01((1f - t) / 0.2f);
                for (int i = 0; i < _refundOrbiterImages.Length; i++)
                {
                    Color color = _refundOrbiterColors[i];
                    color.a *= alpha;
                    _refundOrbiterImages[i].color = color;
                }

                _refundTrailTimer -= deltaTime;
                if (_refundTrailTimer <= 0f)
                {
                    _refundTrailTimer = _refundTrailInterval;
                    SpawnRefundTrail(position);
                }
            }
        }

        float textT = Mathf.Clamp01(_refundTime / _refundTextDuration);
        if (_cleanseRefundText != null && _cleanseRefundText.gameObject.activeSelf)
        {
            if (textT >= 1f)
            {
                _cleanseRefundText.gameObject.SetActive(false);
            }
            else
            {
                float alpha = Mathf.Clamp01(textT / 0.12f) * Mathf.Clamp01((1f - textT) / 0.35f);
                Color color = _refundTextColor;
                color.a *= alpha;
                _cleanseRefundText.color = color;
                _cleanseRefundText.rectTransform.anchoredPosition =
                    _refundTextPosition + Vector2.up * (_refundTextRise * (1f - (1f - textT) * (1f - textT)));
            }
        }

        if (t >= 1f && textT >= 1f)
        {
            _refundTime = -1f;
        }
    }

    private void SpawnRefundTrail(Vector2 position)
    {
        if (_cleanseRefundTrail == null)
        {
            return;
        }

        TrailMote mote = _refundTrails.Find(item => item.Image != null && !item.Image.gameObject.activeSelf);
        if (mote == null)
        {
            Image image = Instantiate(_cleanseRefundTrail, _cleanseRefundTrail.transform.parent);
            mote = new TrailMote { Image = image };
            _refundTrails.Add(mote);
        }

        mote.Age = 0f;
        mote.Image.rectTransform.anchoredPosition = position;
        mote.Image.gameObject.SetActive(true);
    }

    private void UpdateRefundTrails(float deltaTime)
    {
        foreach (TrailMote mote in _refundTrails)
        {
            if (mote.Image == null || !mote.Image.gameObject.activeSelf)
            {
                continue;
            }

            mote.Age += deltaTime;
            float t = mote.Age / _refundTrailLife;
            if (t >= 1f)
            {
                mote.Image.gameObject.SetActive(false);
                continue;
            }

            Color color = _refundTrailColor;
            color.a *= 1f - t;
            mote.Image.color = color;
            mote.Image.rectTransform.localScale = Vector3.one * (1f - 0.5f * t);
        }
    }

    private void PlayMaxUp(int from, int to)
    {
        if (_barrierMaxUpGlow == null && _barrierMaxUpText == null)
        {
            return;
        }

        _maxUpTime = 0f;
        if (_barrierMaxUpGlow != null)
        {
            _barrierMaxUpGlow.gameObject.SetActive(true);
        }

        if (_barrierMaxUpText != null)
        {
            _barrierMaxUpText.text = $"최대 {from} > {to}";
            _barrierMaxUpText.gameObject.SetActive(true);
        }

        UpdateMaxUpFx(0f);
    }

    private void UpdateMaxUpFx(float deltaTime)
    {
        if (_maxUpTime < 0f)
        {
            return;
        }

        _maxUpTime += deltaTime;
        float t = Mathf.Clamp01(_maxUpTime / _maxUpDuration);
        if (t >= 1f)
        {
            _maxUpTime = -1f;
            if (_barrierMaxUpGlow != null)
            {
                _barrierMaxUpGlow.gameObject.SetActive(false);
            }

            if (_barrierMaxUpText != null)
            {
                _barrierMaxUpText.gameObject.SetActive(false);
            }

            return;
        }

        if (_barrierMaxUpGlow != null)
        {
            // 빛났다 잦아들기를 _maxUpPulses번 되풀이하며 점점 약해진다.
            float pulse = 0.5f - 0.5f * Mathf.Cos(t * _maxUpPulses * Mathf.PI * 2f);
            Color color = _maxUpGlowColor;
            color.a *= Mathf.Lerp(0.35f, 1f, pulse) * (1f - t * t);
            _barrierMaxUpGlow.color = color;
            float grow = 1f - Mathf.Clamp01(t * 3f);
            float scale = Mathf.Lerp(1f, _maxUpStartScale, grow * grow);
            _barrierMaxUpGlow.rectTransform.localScale = Vector3.one * scale;
        }

        if (_barrierMaxUpText != null)
        {
            Color color = _maxUpTextColor;
            color.a *= Mathf.Clamp01(t / 0.1f) * Mathf.Clamp01((1f - t) / 0.25f);
            _barrierMaxUpText.color = color;
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
