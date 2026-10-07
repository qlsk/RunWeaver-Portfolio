using UnityEngine;

/// 마지막으로 관측한 대상 위치를 기준으로 접근하거나 최소 거리 안에서 후퇴한다.
[RequireComponent(typeof(EnemyMotor), typeof(EnemyTargetDetector))]
public class RangeEnemyMovement : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private float _minRange = 5f;
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
        if (_targetDetector.Target != null && _enemyData != null && _enemyData.Stat != null)
        {
            Vector2 offset = _targetDetector.LastKnownTargetPosition - (Vector2)transform.position;
            Vector2 direction = offset.normalized;
            if (offset.magnitude < _minRange) direction = -direction;
            {
                velocity = direction * _enemyData.Stat.MoveSpeed;
            }
        }

        _motor.ApplyMovement(velocity);
    }
}