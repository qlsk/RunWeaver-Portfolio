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
    [Tooltip("발자국 간격 (초)")]
    [SerializeField, Min(0.05f)] private float _stepInterval = 0.28f;
    [Tooltip("이 속도(월드 단위/초)보다 빠를 때만 걷는 것으로 본다")]
    [SerializeField, Min(0f)] private float _stepMinSpeed = 0.5f;
    [Tooltip("왼발, 오른발이 옆으로 벌어지는 거리")]
    [SerializeField, Min(0f)] private float _stepSideOffset = 0.08f;

    private Shield _shield;
    private PlayerHealth _health;
    private TraitManager _traits;
    private Transform _playerTransform;

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
        }

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
        Vector3 delta = position - _lastPosition;
        _lastPosition = position;

        // shield_05 : 보호막이 있는 동안 걸으면 발자국을 남긴다.
        float deltaTime = Time.deltaTime;
        if (_traits == null || !_traits.Has("shield_05") || _shield == null || _shield.CurrentShield <= 0 ||
            deltaTime <= 0f || delta.magnitude / deltaTime < _stepMinSpeed)
        {
            _stepTimer = 0f;
            return;
        }

        _stepTimer -= deltaTime;
        if (_stepTimer > 0f)
        {
            return;
        }

        _stepTimer = _stepInterval;
        TraitFx fx = TraitFx.Instance;
        if (fx == null)
        {
            return;
        }

        Vector2 direction = ((Vector2)delta).normalized;
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
            if (enemy == null)
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
