using UnityEngine;

/// 마지막으로 관측한 대상 위치를 기준으로 접근하거나 최소 거리 안에서 후퇴한다.
public class RangeEnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    private EnemyTankAttack _tankAttack;
    private EnemyTargetDetector _targetDetector;
    private Rigidbody2D _enemyRigidbody2D;
    [SerializeField] private float _minRange = 5f;

    private void Start()
    {
        _enemyRigidbody2D = GetComponent<Rigidbody2D>();
        _tankAttack = GetComponent<EnemyTankAttack>();
        _targetDetector = GetComponent<EnemyTargetDetector>();
    }

    private void FixedUpdate()
    {
        if (_tankAttack != null && _tankAttack.IsAttackActive)
            return;

        Transform target = _targetDetector.Target;
        if (target == null)
        {
            _enemyRigidbody2D.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition = _targetDetector.LastKnownTargetPosition;
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        if (Vector2.Distance(transform.position, targetPosition) < _minRange)
            direction = -direction;

        _enemyRigidbody2D.linearVelocity = direction * _enemyData.Stat.MoveSpeed;
    }
}