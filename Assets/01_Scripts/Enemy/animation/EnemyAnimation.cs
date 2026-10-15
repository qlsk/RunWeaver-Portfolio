using UnityEngine;
using MoreMountains.Feedbacks;

public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] private bool _useTankAnimations;
    [SerializeField, Min(0f)] private float _impactShakeAmplitude = 0.3f;
    [SerializeField, Min(0f)] private float _impactShakeDuration = 0.15f;
    [SerializeField, Min(0f)] private float _rockShakeAmplitude = 0.15f;
    [SerializeField, Min(0f)] private float _rockShakeDuration = 0.15f;
    [SerializeField, Min(0f)] private float _dashShakeAmplitude = 0.12f;
    [SerializeField, Min(0.01f)] private float _dashShakeInterval = 0.16f;
    [SerializeField, Min(0.01f)] private float _dashShakeDuration = 0.08f;
    [SerializeField] private bool _reverse = true;
    private float _nextDashShakeTime;
    private Animator _animator;
    private SpriteRenderer _sprite;
    private EnemyTankAttack _tank;
    private EnemyShockwaveAttack _shockwave;
    private EnemyDashAttack _dash;
    private EnemyRockfallAttack _rock;
    private EnemyRangeAttack _range;
    private EnemyTargetDetector _target;
    private BossPatternController _boss;
    private EnemyHealth _health;
    private Rigidbody2D _body;
    private string _playedState;
    private bool _impactSent;
    private bool _clipFinished;

    private void Start()
    {
        _sprite = GetComponentInChildren<SpriteRenderer>();
        _animator = GetComponentInChildren<Animator>();
        _tank = GetComponent<EnemyTankAttack>();
        _shockwave = GetComponent<EnemyShockwaveAttack>();
        _dash = GetComponent<EnemyDashAttack>();
        _rock = GetComponent<EnemyRockfallAttack>();
        _range = GetComponent<EnemyRangeAttack>();
        if (_range != null) _range.ProjectileFired += OnProjectileFired;
        _target = GetComponent<EnemyTargetDetector>();
        _boss = GetComponent<BossPatternController>();
        _health = GetComponent<EnemyHealth>();
        _body = GetComponent<Rigidbody2D>();
        if (!_useTankAnimations || _animator == null) return;
        var relay = _animator.GetComponent<TankAnimationEvents>();
        if (relay == null) relay = _animator.gameObject.AddComponent<TankAnimationEvents>();
        relay.Owner = this;
        if (_tank != null) _tank.AnimationDriven = true;
        if (_rock != null)
        {
            _rock.AnimationDriven = true;
            _rock.RockErupted += OnRockErupted;
        }

        if (Camera.main != null && FindAnyObjectByType<MMCameraShaker>() == null)
            new MMF_CameraShake().AutomaticShakerSetup();
    }

    private void Update()
    {
        if (_useTankAnimations)
        {
            UpdateTankAnimation();
            UpdateDashShake();
        }

        if ((_tank != null && _tank.IsAttackActive) || (_dash != null && _dash.IsDashActive) ||
            (_rock != null && _rock.IsAttackActive)) return;
        if (_sprite != null && _target != null && _target.Target != null)
            _sprite.flipX = _reverse
                ? transform.position.x < _target.Target.position.x
                : transform.position.x > _target.Target.position.x;
    }

    private void OnProjectileFired()
    {
        if (!isActiveAndEnabled || _animator == null) return;
        int hash = Animator.StringToHash("Base Layer.RangeEnemyAttack");
        if (_animator.HasState(0, hash)) _animator.Play(hash, 0, 0f);
    }

    private void UpdateTankAnimation()
    {
        if (_animator == null) return;
        bool tank = _tank != null && _tank.IsAttackActive;
        bool rock = _rock != null && _rock.IsAttackActive;
        string state = "TankIdle";
        float speed = 1f;
        if ((tank && _tank.State == EnemyAttackState.Preparing) || (rock && _rock.State == EnemyAttackState.Preparing))
        {
            state = "TankPrepare";
            speed = (4f / 12f) / (tank ? _tank.WarningDuration : _rock.WarningDuration);
        }
        else if (_shockwave != null && _shockwave.IsAttackActive)
        {
            state = _shockwave.State == EnemyAttackState.Preparing ? "TankPrepare" : "TankAttack";
            speed = _shockwave.State == EnemyAttackState.Preparing ? (4f / 12f) / _shockwave.WarningDuration : 1f;
        }
        else if (tank || rock)
            state = _clipFinished ? "TankIdle" : "TankAttack";
        else if (_dash != null && _dash.IsDashActive)
            state = _dash.State == EnemyAttackState.Preparing ? "TankIdle" : "TankDash";
        else if ((_boss == null || !_boss.IsRecovering) && _body != null && _body.linearVelocity.sqrMagnitude > 0.001f)
            state = "TankEnemyWalk";

        if (_health != null && _health.IsDead) state = "TankIdle";
        _animator.speed = speed;
        if (_playedState == state) return;
        int hash = Animator.StringToHash("Base Layer." + state);
        if (!_animator.HasState(0, hash)) return;
        if (state == "TankPrepare")
        {
            _clipFinished = false;
            _impactSent = false;
        }

        _animator.Play(hash, 0, 0f);
        _playedState = state;
    }

    public void AnimationImpact()
    {
        if (!isActiveAndEnabled || _playedState != "TankAttack" || _impactSent ||
            (_health != null && _health.IsDead)) return;
        bool tank = _tank != null && _tank.IsAttackActive && _tank.State == EnemyAttackState.Attacking;
        bool rock = _rock != null && _rock.IsAttackActive && _rock.State == EnemyAttackState.Attacking;
        if (!tank && !rock) return;
        _impactSent = true;
        Shake(_impactShakeAmplitude, _impactShakeDuration);
        if (tank) _tank.AnimationImpact();
        else _rock.AnimationImpact();
    }

    public void AnimationComplete()
    {
        if (!isActiveAndEnabled || !_impactSent || _playedState != "TankAttack") return;
        _clipFinished = true;
        if (_tank != null) _tank.AnimationComplete();
    }

    private void OnRockErupted() => Shake(_rockShakeAmplitude, _rockShakeDuration);

    private void UpdateDashShake()
    {
        bool moving = _dash != null && _dash.IsDashActive && _dash.State == EnemyAttackState.Attacking &&
                      _body != null && _body.linearVelocity.sqrMagnitude > 0.001f &&
                      (_health == null || !_health.IsDead);
        if (!moving)
        {
            _nextDashShakeTime = 0f;
            return;
        }

        if (Time.timeScale <= 0f || Time.time < _nextDashShakeTime) return;
        _nextDashShakeTime = Time.time + Mathf.Max(0.01f, _dashShakeInterval);
        if (_dashShakeAmplitude <= 0f) return;
        MMCameraShakeEvent.Trigger(Mathf.Max(0.01f, _dashShakeDuration), _dashShakeAmplitude, 30f,
            _dashShakeAmplitude * 0.5f, _dashShakeAmplitude, 0f);
    }

    private static void Shake(float amplitude, float duration)
    {
        if (amplitude <= 0f || duration <= 0f) return;
        MMCameraShakeEvent.Trigger(duration, amplitude, 30f, amplitude, amplitude, 0f);
    }

    private void OnDisable()
    {
        _nextDashShakeTime = 0f;
        if (!_useTankAnimations) return;
        if (_tank != null) _tank.CancelAttack();
        if (_rock != null) _rock.CancelAttack();
        _playedState = null;
        _impactSent = false;
        _clipFinished = false;
    }

    private void OnDestroy()
    {
        if (_range != null) _range.ProjectileFired -= OnProjectileFired;
        if (_rock != null) _rock.RockErupted -= OnRockErupted;
    }
}
