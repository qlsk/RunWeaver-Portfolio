using UnityEngine;

/// 마지막으로 관측한 대상 위치를 기준으로 접근하거나 최소 거리 안에서 후퇴한다.
public class RangeEnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private float _minRange = 5f;

    // Movement references
    private EnemyTargetDetector _targetDetector;
    private Rigidbody2D _enemyRigidbody2D;
    private EnemyStatusEffectController _statusEffectController;

    // Attack references
    private EnemyTankAttack _tankAttack;

    /// 접근·후퇴 이동과 상태 효과 처리에 필요한 컴포넌트 참조를 확보한다.
    private void Start()
    {
        _enemyRigidbody2D = GetComponent<Rigidbody2D>();
        _tankAttack = GetComponent<EnemyTankAttack>();
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _statusEffectController = GetComponent<EnemyStatusEffectController>();
    }

    /// 접근·후퇴에 넉백, 당김과 밀어내기를 합산하며 당김 중 추적을 멈추고 탱크 공격 중 속도 제어를 유지한다.
    private void FixedUpdate()
    {
        if (_tankAttack != null && _tankAttack.IsAttackActive)
            return;

        Transform target = _targetDetector.Target;
        bool isPulled = _statusEffectController != null && _statusEffectController.IsPulled;
        bool isPushed = _statusEffectController != null && _statusEffectController.IsPushed;
        if (target == null && !isPulled && !isPushed)
        {
            _enemyRigidbody2D.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 moveVelocity = Vector2.zero;
        if (target != null && !isPulled)
        {
            Vector2 targetPosition = _targetDetector.LastKnownTargetPosition;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            if (Vector2.Distance(transform.position, targetPosition) < _minRange)
                direction = -direction;

            float speedMultiplier = _statusEffectController != null ? _statusEffectController.MoveSpeedMultiplier : 1f;
            moveVelocity = direction * (_enemyData.Stat.MoveSpeed * speedMultiplier);
        }

        Vector2 knockback = _statusEffectController != null ? _statusEffectController.KnockbackVelocity : Vector2.zero;
        Vector2 pull = _statusEffectController != null ? _statusEffectController.PullVelocity : Vector2.zero;
        Vector2 push = _statusEffectController != null ? _statusEffectController.PushVelocity : Vector2.zero;
        _enemyRigidbody2D.linearVelocity = moveVelocity + knockback + pull + push;
    }
}
