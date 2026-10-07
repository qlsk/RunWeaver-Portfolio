using UnityEngine;

/// 마지막으로 관측한 대상 위치를 향한 일반 이동을 요청한다.
[RequireComponent(typeof(EnemyMovementController), typeof(EnemyTargetDetector))]
public class MeleeEnemyMovement : MonoBehaviour, IEnemyMovementBehavior
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private bool _enemyMove = true;
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
        if (!_enemyMove || _targetDetector == null || _targetDetector.Target == null || _enemyData == null ||
            _enemyData.Stat == null)
        {
            return new MovementGoal { Destination = position, ShouldStop = true };
        }

        Vector2 destination = _targetDetector.LastKnownTargetPosition;
        return new MovementGoal
        {
            Destination = destination,
            StoppingDistance = 0f,
            ShouldStop = false
        };
    }
}