using UnityEngine;

/// 기존 공격의 시작 순서와 보스 전용 후딜레이를 관리한다.
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth), typeof(EnemyTargetDetector), typeof(EnemyMotor))]
public class BossPatternController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _patternInterval = 0.5f;
    [SerializeField, Min(0f)] private float _tankRecoveryDuration = 1.2f;
    [SerializeField, Min(0f)] private float _dashRecoveryDuration = 0.7f;
    [SerializeField, Min(0f)] private float _wallRecoveryDuration = 2f;
    [SerializeField, Range(1, 3)] private int _dashMinimumPhase = 1;
    private bool _nextFarAttackIsRockfall;
    [SerializeField, Min(0f)] private float _rockfallRecoveryDuration = 1f;
    [SerializeField, Range(1, 3)] private int _rockfallMinimumPhase = 1;
    [SerializeField, Range(1, 3)] private int _tankMinimumPhase = 1;
    private EnemyTankAttack _tank;
    private EnemyDashAttack _dash;
    private EnemyRockfallAttack _rockfall;
    private EnemyMotor _motor;
    private EnemyHealth _health;
    private EnemyTargetDetector _targetDetector;
    private EnemyStatusEffectController _statusEffects;
    private MonoBehaviour _activeAttack;
    private float _nextPatternTime;
    private float _recoveryEndTime;

    public int Phase { get; private set; } = 1;
    public MonoBehaviour ActiveAttack => _activeAttack;
    public bool IsRecovering { get; private set; }

    private void Awake()
    {
        _tank = GetComponent<EnemyTankAttack>();
        _dash = GetComponent<EnemyDashAttack>();
        _rockfall = GetComponent<EnemyRockfallAttack>();
        _motor = GetComponent<EnemyMotor>();
        _health = GetComponent<EnemyHealth>();
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _statusEffects = GetComponentInParent<EnemyStatusEffectController>();
        SetExternalControl();
    }

    private void OnEnable()
    {
        SetExternalControl();
        if (_tank != null) _tank.AttackEnded += HandleTankEnded;
        if (_dash != null) _dash.AttackEnded += HandleDashEnded;
        if (_rockfall != null) _rockfall.AttackEnded += HandleRockfallEnded;
        EnemyHealth.OnDeath += HandleDeath;
    }

    private void SetExternalControl()
    {
        // 관리자 비활성화 시에도 자동 공격으로 되돌리지 않는다.
        if (_tank != null) _tank.SetExternalControl(true);
        if (_dash != null) _dash.SetExternalControl(true);
        if (_rockfall != null) _rockfall.SetExternalControl(true);
    }

    private void Start()
    {
        if (_tank == null && _rockfall == null)
            Debug.LogWarning("BossPatternController: 같은 오브젝트에 보스가 사용할 공격 컴포넌트를 추가하세요.", this);
        foreach (EnemyRangeAttack attack in GetComponents<EnemyRangeAttack>())
        {
            if (!attack.isActiveAndEnabled) continue;
            Debug.LogError("BossPatternController: 별도로 자동 실행되는 EnemyRangeAttack을 비활성화하세요.", this);
            enabled = false;
            break;
        }
    }

    private void FixedUpdate()
    {
        if (_health.IsDead || _targetDetector.Target == null ||
            (_statusEffects != null && _statusEffects.IsStaggered))
        {
            CancelAll();
            return;
        }
        if (_activeAttack != null) return;
        if (IsRecovering)
        {
            if (Time.time < _recoveryEndTime) return;
            IsRecovering = false;
            _motor.ReleasePositionLock(this);
            _nextPatternTime = Time.time + _patternInterval;
            return;
        }
        if (_health.MaxHealth <= 0f || Time.time < _nextPatternTime) return;

        float ratio = _health.CurrentHealth / _health.MaxHealth;
        Phase = ratio <= 0.3f ? 3 : ratio <= 0.6f ? 2 : 1;
        // 쿨타임 때문에 근거리 공격 대신 낙석이 선택되지 않도록 거리를 먼저 판정
        Vector2 offset = _targetDetector.Target.position - transform.position;
        bool isClose = _tank != null && offset.sqrMagnitude <= _tank.DetectionRange * _tank.DetectionRange;
        if (isClose) TryTank();
        else if (_nextFarAttackIsRockfall) TryRockfall();
        else TryDash();
    }

    private bool TryDash()
    {
        if (_dash == null || Phase < _dashMinimumPhase || !_dash.TryStartAttack()) return false;
        _activeAttack = _dash;
        _nextFarAttackIsRockfall = true;
        return true;
    }

    private void HandleDashEnded(EnemyAttackEndReason reason)
    {
        if (_activeAttack == _dash)
            FinishAttack(reason, reason == EnemyAttackEndReason.Obstacle ? _wallRecoveryDuration : _dashRecoveryDuration);
    }

    private bool TryTank()
    {
        if (_tank == null || Phase < _tankMinimumPhase || !_tank.TryStartAttack()) return false;
        _activeAttack = _tank;
        return true;
    }

    private bool TryRockfall()
    {
        if (_rockfall == null || Phase < _rockfallMinimumPhase || !_rockfall.TryStartAttack()) return false;
        _activeAttack = _rockfall;
        _nextFarAttackIsRockfall = false;
        return true;
    }

    private void HandleRockfallEnded(EnemyAttackEndReason reason)
    {
        if (_activeAttack == _rockfall) FinishAttack(reason, _rockfallRecoveryDuration);
    }

    private void HandleTankEnded(EnemyAttackEndReason reason)
    {
        if (_activeAttack == _tank) FinishAttack(reason, _tankRecoveryDuration);
    }

    private void FinishAttack(EnemyAttackEndReason reason, float recovery)
    {
        _activeAttack = null;
        if (reason == EnemyAttackEndReason.Cancelled || !isActiveAndEnabled || _health.IsDead)
        {
            _nextPatternTime = Time.time + _patternInterval;
            return;
        }
        IsRecovering = true;
        _recoveryEndTime = Time.time + recovery;
        _motor.AcquirePositionLock(this);
    }

    private void CancelAll()
    {
        if (_activeAttack != null || IsRecovering)
            _nextPatternTime = Time.time + _patternInterval;
        _activeAttack = null;
        if (_tank != null) _tank.CancelAttack();
        if (_dash != null) _dash.CancelDash();
        if (_rockfall != null) _rockfall.CancelAttack();
        IsRecovering = false;
        _motor?.ReleasePositionLock(this);
    }

    private void HandleDeath(EnemyHealth health)
    {
        if (health == _health) CancelAll();
    }

    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleDeath;
        if (_tank != null) _tank.AttackEnded -= HandleTankEnded;
        if (_dash != null) _dash.AttackEnded -= HandleDashEnded;
        if (_rockfall != null) _rockfall.AttackEnded -= HandleRockfallEnded;
        CancelAll();
    }
}
