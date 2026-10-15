using UnityEngine;

/// 기존 공격의 시작 순서와 패턴 간격을 관리한다.
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth), typeof(EnemyTargetDetector), typeof(EnemyMotor))]
public class BossPatternController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _patternInterval = 0.5f;
    [SerializeField, Range(1, 3)] private int _dashMinimumPhase = 1;
    private bool _nextFarAttackIsRockfall;
    [SerializeField, Range(1, 3)] private int _rockfallMinimumPhase = 1;
    [SerializeField, Range(1, 3)] private int _tankMinimumPhase = 1;
    private EnemyTankAttack _tank;
    private EnemyShockwaveAttack _shockwave;
    [SerializeField, Range(1, 3)] private int _shockwaveMinimumPhase = 2;
    private EnemyDashAttack _dash;
    private EnemyRockfallAttack _rockfall;
    private EnemyHealth _health;
    private EnemyTargetDetector _targetDetector;
    private EnemyStatusEffectController _statusEffects;
    private MonoBehaviour _activeAttack;
    private float _nextPatternTime;
    private Rigidbody2D _body;
    private float _stepTimer;
    private const float AutomaticRockfallInterval = 5f;
    private bool _automaticRockfallEnabled;
    private float _nextAutomaticRockfallTime;

    public int Phase { get; private set; } = 1;
    public MonoBehaviour ActiveAttack => _activeAttack;

    private void Awake()
    {
        _tank = GetComponent<EnemyTankAttack>();
        _shockwave = GetComponent<EnemyShockwaveAttack>();
        _dash = GetComponent<EnemyDashAttack>();
        _rockfall = GetComponent<EnemyRockfallAttack>();
        _health = GetComponent<EnemyHealth>();
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _statusEffects = GetComponentInParent<EnemyStatusEffectController>();
        _body = GetComponent<Rigidbody2D>();
        SetExternalControl();
    }

    private void OnEnable()
    {
        _automaticRockfallEnabled = false;
        SetExternalControl();
        if (_tank != null) _tank.AttackEnded += HandleTankEnded;
        if (_dash != null) _dash.AttackEnded += HandleDashEnded;
        if (_rockfall != null) _rockfall.AttackEnded += HandleRockfallEnded;
        EnemyHealth.OnDeath += HandleDeath;
        if (_shockwave != null) _shockwave.AttackEnded += HandleShockwaveEnded;
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
        if (_health.MaxHealth > 0f)
        {
            float ratio = _health.CurrentHealth / _health.MaxHealth;
            Phase = ratio <= 0.4f ? 3 : ratio <= 0.7f ? 2 : 1;
        }
        if (_health.IsDead || _targetDetector.Target == null ||
            (_statusEffects != null && _statusEffects.IsStaggered))
        {
            CancelAll();
            return;
        }
        UpdateAutomaticRockfall();
        if (_activeAttack != null) return;
        UpdateFootsteps();
        if (_health.MaxHealth <= 0f || Time.time < _nextPatternTime) return;

        if (_shockwave != null && Phase >= _shockwaveMinimumPhase && _shockwave.TryStartAttack())
        {
            _activeAttack = _shockwave;
            Sfx.Play(Sfx.BossTryShockwave);
            return;
        }
        // 쿨타임 때문에 근거리 공격 대신 낙석이 선택되지 않도록 거리를 먼저 판정
        Vector2 offset = _targetDetector.Target.position - transform.position;
        bool isClose = _tank != null && offset.sqrMagnitude <= _tank.DetectionRange * _tank.DetectionRange;
        if (isClose) TryTank();
        else if (Phase < 2 && _nextFarAttackIsRockfall) TryRockfall();
        else TryDash();
    }

    private bool TryDash()
    {
        if (_dash == null || Phase < _dashMinimumPhase || !_dash.TryStartAttack()) return false;
        _activeAttack = _dash;
        _nextFarAttackIsRockfall = true;
        Sfx.Play(Sfx.BossTryDash);
        return true;
    }

    private void HandleDashEnded(EnemyAttackEndReason reason)
    {
        if (_activeAttack == _dash)
            FinishAttack();
    }

    private bool TryTank()
    {
        if (_tank == null || Phase < _tankMinimumPhase || !_tank.TryStartAttack(Phase >= 2 ? 1.3f : 1f)) return false;
        _activeAttack = _tank;
        Sfx.Play(Sfx.BossTryTank);
        return true;
    }

    private bool TryRockfall()
    {
        if (_rockfall == null || Phase < _rockfallMinimumPhase || Phase >= 2) return false;
        if (!_rockfall.TryStartAttack()) return false;
        _activeAttack = _rockfall;
        _nextFarAttackIsRockfall = false;
        Sfx.Play(Sfx.BossTryRockfall);
        return true;
    }

    private void UpdateAutomaticRockfall()
    {
        if (Phase < 2)
        {
            _automaticRockfallEnabled = false;
            return;
        }
        if (!_automaticRockfallEnabled)
        {
            _automaticRockfallEnabled = true;
            _nextAutomaticRockfallTime = Time.time + AutomaticRockfallInterval;
            if (_rockfall != null) _rockfall.CancelAttack();
        }
        if (Time.time < _nextAutomaticRockfallTime) return;
        _nextAutomaticRockfallTime = Time.time + AutomaticRockfallInterval;
        if (_rockfall != null) _rockfall.TryStartAutomaticAttack(Phase);
    }

    private void HandleRockfallEnded(EnemyAttackEndReason reason)
    {
        if (_activeAttack == _rockfall) FinishAttack();
    }

    private void HandleTankEnded(EnemyAttackEndReason reason)
    {
        if (_activeAttack == _tank) FinishAttack();
    }

    private void HandleShockwaveEnded(EnemyAttackEndReason reason)
    {
        if (_activeAttack == _shockwave) FinishAttack();
    }

    private void FinishAttack()
    {
        _activeAttack = null;
        _nextPatternTime = Time.time + _patternInterval;
    }

    /// <summary>보스 이동 발걸음. 패턴 중이 아닐 때만 실제 속도에 맞춰 울린다.</summary>
    private void UpdateFootsteps()
    {
        float speed = _body != null ? _body.linearVelocity.magnitude : 0f;
        if (speed < 0.3f)
        {
            _stepTimer = 0f;
            return;
        }

        _stepTimer -= Time.fixedDeltaTime;
        if (_stepTimer <= 0f)
        {
            Sfx.Play(Sfx.BossFootstep);
            _stepTimer = Mathf.Clamp(1.75f / speed, 0.5f, 2f);
        }
    }

    private void CancelAll()
    {
        if (_activeAttack != null)
            _nextPatternTime = Time.time + _patternInterval;
        _activeAttack = null;
        if (_tank != null) _tank.CancelAttack();
        if (_shockwave != null) _shockwave.CancelAttack();
        if (_dash != null) _dash.CancelDash();
        if (_rockfall != null) _rockfall.CancelAttack();
    }

    private void HandleDeath(EnemyHealth health)
    {
        if (health == _health) CancelAll();
    }

    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleDeath;
        if (_shockwave != null) _shockwave.AttackEnded -= HandleShockwaveEnded;
        if (_tank != null) _tank.AttackEnded -= HandleTankEnded;
        if (_dash != null) _dash.AttackEnded -= HandleDashEnded;
        if (_rockfall != null) _rockfall.AttackEnded -= HandleRockfallEnded;
        CancelAll();
    }
}
