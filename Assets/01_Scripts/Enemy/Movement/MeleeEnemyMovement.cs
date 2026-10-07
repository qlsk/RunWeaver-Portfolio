using UnityEngine;

/// 마지막으로 관측한 대상 위치를 향한 일반 이동을 요청한다.
[RequireComponent(typeof(EnemyMotor), typeof(EnemyTargetDetector))]
public class MeleeEnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private bool _enemyMove = true;
    private EnemyTargetDetector _targetDetector;
    private EnemyMotor _motor;

    private void Awake()
    {
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _motor = GetComponent<EnemyMotor>();
    }

    private void FixedUpdate()
    {
        Vector2 velocity = Vector2.zero;
        if (_enemyMove && _targetDetector.Target != null && _enemyData != null && _enemyData.Stat != null)
        {
            Vector2 direction = (_targetDetector.LastKnownTargetPosition - (Vector2)transform.position).normalized;
            velocity = direction * _enemyData.Stat.MoveSpeed;
        }

        _motor.ApplyMovement(velocity);
    }
}