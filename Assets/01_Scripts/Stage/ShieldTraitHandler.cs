using UnityEngine;

/// <summary>
/// shield 카테고리 핸들러. 발동 지점: ShieldBroken(실파괴).
///
/// - shield_04: 파괴 시 주변 적 밀쳐내기 (ApplyKnockback 직접 호출).
/// - shield_01/02/03/05 수치형은 TraitStatComposer가 처리한다.
/// 연출(TraitFx) : shield_04 가시 충격파 + 파편 + 적 넉백 이펙트, shield_02 무적 동안 파편이 맴돎,
///   shield_05 보호막이 있는 동안 걸으면 금빛 발자국.
/// </summary>
[DisallowMultipleComponent]
public class ShieldTraitHandler : MonoBehaviour
{
    [Header("shield_04 파열의 고리")]
    [SerializeField, Min(0f)] private float _ruptureKnockbackSpeed = 12f;
    [SerializeField, Min(0f)] private float _ruptureKnockbackDuration = 0.3f;

    [Header("shield_05 수호자의 걸음 (발자국 연출)")]
    [Tooltip("플레이어 몸통 애니메이터의 달리기 상태 이름")]
    [SerializeField] private string _runStateName = "Run";
    [Tooltip("달리기 클립에서 발이 땅에 닿는 순간 (0~1, 클립 길이 비율). 클립 한 번에 두 걸음")]
    [SerializeField] private float[] _footfallPhases = { 0.42f, 0.92f };
    [Tooltip("애니메이터를 못 찾았을 때 쓰는 발자국 간격 (초)")]
    [SerializeField, Min(0.05f)] private float _fallbackStepInterval = 0.2f;
    [Tooltip("이 속도(월드 단위/초)보다 빠를 때만 걷는 것으로 본다")]
    [SerializeField, Min(0f)] private float _stepMinSpeed = 0.5f;
    [Tooltip("왼발, 오른발이 옆으로 벌어지는 거리")]
    [SerializeField, Min(0f)] private float _stepSideOffset = 0.08f;

    private Shield _shield;
    private PlayerHealth _health;
    private TraitManager _traits;
    private Transform _playerTransform;

    private Rigidbody2D _playerBody;
    private Animator _bodyAnimator;
    private int _runStateHash;
    private float _lastRunTime = float.NaN;
    private Vector3 _lastPosition;
    private float _stepTimer;
    private bool _isLeftStep;

    private void Start()
    {
        _shield = FindAnyObjectByType<Shield>();
        _health = FindAnyObjectByType<PlayerHealth>();
        _traits = TraitManager.Instance;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            _lastPosition = _playerTransform.position;
            _playerBody = player.GetComponentInChildren<Rigidbody2D>();
        }

        // 달리기 애니메이션(PlayerAnimation이 붙은 몸통)의 재생 위치에 발자국을 맞춘다.
        PlayerAnimation bodyAnimation = FindAnyObjectByType<PlayerAnimation>();
        if (bodyAnimation != null)
        {
            _bodyAnimator = bodyAnimation.GetComponent<Animator>();
        }

        _runStateHash = Animator.StringToHash(_runStateName);

        if (_shield != null)
        {
            _shield.ShieldBroken -= HandleShieldBroken;
            _shield.ShieldBroken += HandleShieldBroken;
        }
    }

    private void OnDestroy()
    {
        if (_shield != null)
        {
            _shield.ShieldBroken -= HandleShieldBroken;
        }
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            return;
        }

        Vector3 position = _playerTransform.position;
        Vector2 velocity = _playerBody != null
            ? _playerBody.linearVelocity
            : (Vector2)(position - _lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPosition = position;

        // shield_05 : 보호막이 있는 동안 걸으면 발이 땅에 닿을 때마다 발자국을 남긴다.
        bool active = _traits != null && _traits.Has("shield_05") && _shield != null && _shield.CurrentShield > 0 &&
                      Time.deltaTime > 0f && velocity.magnitude >= _stepMinSpeed;
        if (_bodyAnimator != null && _bodyAnimator.isActiveAndEnabled)
        {
            UpdateStepsByAnimation(active, velocity);
        }
        else
        {
            UpdateStepsByTimer(active, velocity);
        }
    }

    private void UpdateStepsByAnimation(bool active, Vector2 velocity)
    {
        AnimatorStateInfo state = _bodyAnimator.GetCurrentAnimatorStateInfo(0);
        if (!active || state.shortNameHash != _runStateHash)
        {
            _lastRunTime = float.NaN;
            return;
        }

        // normalizedTime은 반복할수록 커진다 (뒷걸음질로 역재생하면 작아진다).
        float time = state.normalizedTime;
        if (!float.IsNaN(_lastRunTime) && _footfallPhases != null)
        {
            float low = Mathf.Min(_lastRunTime, time);
            float high = Mathf.Max(_lastRunTime, time);
            foreach (float phase in _footfallPhases)
            {
                // low < phase + n <= high 인 정수 n이 있으면 이번 프레임에 발이 닿았다.
                if (Mathf.Floor(high - phase) > Mathf.Floor(low - phase))
                {
                    SpawnStep(velocity);
                }
            }
        }

        _lastRunTime = time;
    }

    private void UpdateStepsByTimer(bool active, Vector2 velocity)
    {
        if (!active)
        {
            _stepTimer = 0f;
            return;
        }

        _stepTimer -= Time.deltaTime;
        if (_stepTimer > 0f)
        {
            return;
        }

        _stepTimer = _fallbackStepInterval;
        SpawnStep(velocity);
    }

    private void SpawnStep(Vector2 velocity)
    {
        TraitFx fx = TraitFx.Instance;
        if (fx == null)
        {
            return;
        }

        Vector2 direction = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : Vector2.right;
        Vector2 side = new Vector2(-direction.y, direction.x) * (_isLeftStep ? _stepSideOffset : -_stepSideOffset);
        _isLeftStep = !_isLeftStep;
        fx.SpawnWardStep(fx.PlayerFeet(_playerTransform) + (Vector3)side);
    }

    private void HandleShieldBroken()
    {
        if (_traits == null || _playerTransform == null)
        {
            return;
        }

        TraitFx fx = TraitFx.Instance;

        // shield_02 : 막아 낸 뒤 늘어난 무적 시간 동안 파편이 맴돈다.
        if (_traits.Has("shield_02") && _health != null && fx != null)
        {
            float duration = _health.InvincibilityDuration * Mathf.Max(0f, _health.ShieldBlockInvincibilityMultiplier);
            fx.PlayWardLinger(_playerTransform, duration);
        }

        if (!_traits.Has("shield_04"))
        {
            return;
        }

        float radius = 4f;
        if (_traits.TryGet("shield_04", out TraitData data) && data.Radius > 0f)
        {
            radius = data.Radius;
        }

        if (fx != null)
        {
            fx.PlayRupture(_playerTransform);
        }

        Vector2 center = _playerTransform.position;
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            Vector2 toEnemy = (Vector2)enemy.transform.position - center;
            if (toEnemy.sqrMagnitude > radius * radius)
            {
                continue;
            }

            EnemyStatusEffectController status = enemy.GetComponent<EnemyStatusEffectController>();
            if (status == null)
            {
                status = enemy.GetComponentInChildren<EnemyStatusEffectController>();
            }

            if (status != null)
            {
                Vector2 direction = toEnemy.sqrMagnitude > 0.0001f ? toEnemy.normalized : Vector2.up;
                status.ApplyKnockbackWithVisual(direction, _ruptureKnockbackSpeed, _ruptureKnockbackDuration);
            }
        }
    }
}
