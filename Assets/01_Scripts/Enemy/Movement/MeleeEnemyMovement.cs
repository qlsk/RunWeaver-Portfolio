using UnityEngine;

/// 마지막으로 관측한 대상 위치를 향해 이동하고 이동 상태 효과를 적용한다.
public class MeleeEnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private bool _enemyMove = true;

    // Movement references
    private EnemyTargetDetector _targetDetector;
    private Rigidbody2D _enemyRigidbody2D;
    private EnemyStatusEffectController _statusEffectController;

    // Attack references
    private EnemyTankAttack _tankAttack;
    private EnemyDashAttack _dashAttack;

    /// 이동, 상태 효과 및 공격 상태 확인에 필요한 컴포넌트 참조를 확보한다.
    private void Awake()
    {
        _tankAttack = GetComponent<EnemyTankAttack>();
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _enemyRigidbody2D = GetComponent<Rigidbody2D>();
        _dashAttack = GetComponent<EnemyDashAttack>();
        _statusEffectController = GetComponent<EnemyStatusEffectController>();
    }

    /// 추적 이동에 넉백과 당김을 합산하되 당김 중 추적을 멈추고 공격·대시 중 기존 속도 제어를 유지한다.
    private void FixedUpdate()
    {
        if (_tankAttack != null && _tankAttack.IsAttackActive)
            return;

        if (_dashAttack != null && _dashAttack.IsDashActive)
            return;

        Vector2 moveVelocity = Vector2.zero;
        Transform target = _targetDetector.Target;
        bool isStaggered = _statusEffectController != null && _statusEffectController.IsStaggered;
        bool isPulled = _statusEffectController != null && _statusEffectController.IsPulled;
        if (_enemyMove && !isStaggered && !isPulled && target != null && _enemyData != null && _enemyData.Stat != null)
        {
            Vector2 direction = (_targetDetector.LastKnownTargetPosition - (Vector2)transform.position).normalized;
            float speedMultiplier = _statusEffectController != null ? _statusEffectController.MoveSpeedMultiplier : 1f;
            moveVelocity = direction * (_enemyData.Stat.MoveSpeed * speedMultiplier);
        }

        Vector2 knockback = _statusEffectController != null ? _statusEffectController.KnockbackVelocity : Vector2.zero;
        Vector2 pull = _statusEffectController != null ? _statusEffectController.PullVelocity : Vector2.zero;
        _enemyRigidbody2D.linearVelocity = moveVelocity + knockback + pull;
    }
}
