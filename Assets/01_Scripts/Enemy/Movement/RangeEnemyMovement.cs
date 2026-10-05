using UnityEngine;

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

        Vector2 direction = (target.position - transform.position).normalized;
        if (Vector2.Distance(transform.position, target.position) < _minRange)
        {
            direction = -direction;
        }

        _enemyRigidbody2D.linearVelocity = direction * _enemyData.Stat.MoveSpeed;
    }
}