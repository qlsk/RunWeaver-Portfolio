using UnityEngine;

/// 마지막으로 관측한 대상 위치를 기준으로 접근하거나 최소 거리 안에서 후퇴한다.
[RequireComponent(typeof(EnemyMovementController), typeof(EnemyTargetDetector))]
public class RangeEnemyMovement : MonoBehaviour, IEnemyMovementBehavior
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private float _minRange = 5f;
    private EnemyTargetDetector _targetDetector;
    public float MoveSpeed => _enemyData != null && _enemyData.Stat != null ? _enemyData.Stat.MoveSpeed : 0f;

    private void Awake()
    {
        _targetDetector = GetComponent<EnemyTargetDetector>();
        // 기존 프리팹에도 새 Controller를 연결한다.
        if (!TryGetComponent<EnemyMovementController>(out _))
        {
            gameObject.AddComponent<EnemyMovementController>();
        }
    }

    /// 목적지와 정지 여부만 결정하며 실제 이동은 수행하지 않는다.
    public MovementGoal CalculateGoal()
    {
        Vector2 position = transform.position;
        if (_targetDetector == null || _targetDetector.Target == null || _enemyData == null || _enemyData.Stat == null)
        {
            return new MovementGoal { Destination = position, ShouldStop = true };
        }

        Vector2 targetPosition = _targetDetector.LastKnownTargetPosition;
        Vector2 offset = targetPosition - position;
        // 최소 거리 안에서는 대상의 반대편을 목적지로 삼아 기존 후퇴 방향을 유지한다.
        Vector2 destination = offset.magnitude < _minRange ? position - offset : targetPosition;
        return new MovementGoal
        {
            Destination = destination,
            StoppingDistance = 0f,
            ShouldStop = false
        };
    }
}