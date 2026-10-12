using UnityEngine;

[RequireComponent(typeof(EnemyTargetDetector))]
public class EnemyRangeAttack : MonoBehaviour
{
    public event System.Action ProjectileFired;

    [SerializeField] private EnemyBullet _bulletPrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField, Min(0f)] private float _attackRange = 7f;
    [SerializeField, Min(0.01f)] private float _attackCooldown = 2f;

    private EnemyTargetDetector _targetDetector;
    private EnemyHealth _health;
    private EnemyStatusEffectController _statusEffectController;
    private float _nextAttackTime;

    private void Awake()
    {
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _health = GetComponent<EnemyHealth>();
        _statusEffectController = GetComponentInParent<EnemyStatusEffectController>();
    }

    private void Update()
    {
        if (_bulletPrefab == null || Time.time < _nextAttackTime)
            return;

        if ((_health != null && _health.IsDead) ||
            (_statusEffectController != null && _statusEffectController.IsStaggered))
            return;

        Transform target = _targetDetector.Target;
        if (target == null)
            return;

        Vector2 targetOffset = target.position - transform.position;
        if (targetOffset.sqrMagnitude > _attackRange * _attackRange)
            return;

        Vector3 firePosition = _firePoint != null ? _firePoint.position : transform.position;
        Vector2 direction = (Vector2)(target.position - firePosition);
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return;

        EnemyBullet bullet = Instantiate(_bulletPrefab, firePosition, Quaternion.identity);
        bullet.Initialize(direction);
        _nextAttackTime = Time.time + _attackCooldown;
        ProjectileFired?.Invoke();
    }
}
