using UnityEngine;

/// 마지막으로 관측한 대상 위치를 향해 이동하고 이동 상태 효과를 적용한다.
public class MeleeEnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    private EnemyTankAttack _tankAttack;
    private EnemyTargetDetector _targetDetector;
    private Rigidbody2D _enemyRigidbody2D;
    private EnemyDashAttack _dashAttack;
    [SerializeField] private bool _enemyMove = true;
    private EnemyStatusEffectController _statusEffectController;

    private void Awake()
    {
        _tankAttack = GetComponent<EnemyTankAttack>();
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _enemyRigidbody2D = GetComponent<Rigidbody2D>();
        _dashAttack = GetComponent<EnemyDashAttack>();
        _statusEffectController = GetComponent<EnemyStatusEffectController>();
    }

    private void FixedUpdate()
    {
        if (_tankAttack != null && _tankAttack.IsAttackActive)
            return;

        if (_dashAttack != null && _dashAttack.IsDashActive)
            return;

        Vector2 moveVelocity = Vector2.zero;
        Transform target = _targetDetector.Target;
        bool isStaggered = _statusEffectController != null && _statusEffectController.IsStaggered;
        if (_enemyMove && !isStaggered && target != null && _enemyData != null && _enemyData.Stat != null)
        {
            Vector2 direction = (_targetDetector.LastKnownTargetPosition - (Vector2)transform.position).normalized;
            float speedMultiplier = _statusEffectController != null ? _statusEffectController.MoveSpeedMultiplier : 1f;
            moveVelocity = direction * (_enemyData.Stat.MoveSpeed * speedMultiplier);
        }

        Vector2 knockback = _statusEffectController != null ? _statusEffectController.KnockbackVelocity : Vector2.zero;
        _enemyRigidbody2D.linearVelocity = moveVelocity + knockback;
    }
}