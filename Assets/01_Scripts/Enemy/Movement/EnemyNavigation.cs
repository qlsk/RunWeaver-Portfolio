using System.Collections.Generic;
using UnityEngine;

/// 자신의 경로를 갱신하고 다음 경유 지점으로 향하는 희망 속도를 반환한다.
public class EnemyNavigation : MonoBehaviour
{
    [Tooltip("씬에 배치된 Grid를 연결하세요. 비어 있거나 프리팹 에셋이면 씬에서 자동 선택합니다.")]
    [SerializeField] private EnemyNavigationGrid _grid;
    [SerializeField] private float _agentRadius = 0.3f;
    [SerializeField] private float _repathInterval = 0.4f;
    [Tooltip("Grid를 사용하지 않고 목적지로 직진합니다. 장애물 통과에는 별도로 충돌 제외 설정이 필요합니다.")]
    [SerializeField] private bool _ignoreObstacles;
    private readonly EnemyPathfinder _pathfinder = new EnemyPathfinder();
    private readonly List<Vector2> _path = new List<Vector2>();
    private readonly List<Collider2D> _colliders = new List<Collider2D>();
    private readonly RaycastHit2D[] _escapeHits = new RaycastHit2D[16];
    private int _waypointIndex, _gridVersion = -1;
    private float _nextRepathTime;
    private bool _warnedMissingGrid;
    private Vector2 _destination;
    private Rigidbody2D _body;
    private float _requiredRadius;
    private Vector2 _navigationOffset;

    private void Start()
    {
        _body = GetComponent<Rigidbody2D>();
        RefreshRequiredRadius();
    }

    private void RefreshRequiredRadius()
    {
        float requiredRadius = Mathf.Max(0.01f, _agentRadius);
        // 몸체 중심에서 경로를 검사한다. 루트와 몸체 사이의 Offset은 크기에 더하지 않는다.
        Bounds bodyBounds = default;
        bool hasBodyBounds = false;
        GetComponentsInChildren<Collider2D>(false, _colliders);
        foreach (Collider2D collider in _colliders)
        {
            if (!collider.enabled || collider.isTrigger || collider.attachedRigidbody != _body)
            {
                continue;
            }

            if (!hasBodyBounds)
            {
                bodyBounds = collider.bounds;
                hasBodyBounds = true;
            }
            else
            {
                bodyBounds.Encapsulate(collider.bounds);
            }
        }

        Vector2 navigationOffset = Vector2.zero;
        if (hasBodyBounds)
        {
            navigationOffset = (Vector2)bodyBounds.center - (Vector2)transform.position;
            Vector2 extent = bodyBounds.extents;
            requiredRadius = Mathf.Max(requiredRadius, extent.magnitude);
        }

        if (Mathf.Approximately(_requiredRadius, requiredRadius) &&
            (navigationOffset - _navigationOffset).sqrMagnitude <= 0.00000001f) return;

        _requiredRadius = requiredRadius;
        _navigationOffset = navigationOffset;
        _warnedMissingGrid = false;
        ClearPath();
        if (_grid != null && _grid.AgentRadius < _requiredRadius)
        {
            _grid = null;
            _gridVersion = -1;
        }

        if (_agentRadius < _requiredRadius)
        {
            float minimumRadius = Mathf.Ceil(_requiredRadius * 1000f) / 1000f;
            Debug.LogWarning(
                $"EnemyNavigation ({name}): Inspector의 Agent Radius({_agentRadius})가 필요한 반지름보다 작습니다. " +
                $"Agent Radius를 최소 {minimumRadius:F3} 이상으로 설정하세요. (Offset 제외, 월드 크기 기준)", this);
        }
    }

    public Vector2 CalculateVelocity(MovementGoal goal, float moveSpeed)
    {
        if (isActiveAndEnabled) RefreshRequiredRadius();

        if (!isActiveAndEnabled || goal.ShouldStop || moveSpeed <= 0f)
        {
            ClearPath();
            return Vector2.zero;
        }

        Vector2 position = _body != null ? _body.position : (Vector2)transform.position;
        if (_ignoreObstacles)
        {
            ClearPath();
            Vector2 toDestination = goal.Destination - position;
            float remainingDistance = Mathf.Max(0f,
                toDestination.magnitude - Mathf.Max(0f, goal.StoppingDistance));
            return toDestination.normalized * Mathf.Min(moveSpeed, remainingDistance / Time.fixedDeltaTime);
        }

        position += _navigationOffset;
        Vector2 destination = goal.Destination + _navigationOffset;

        if (!ResolveGrid(position))
        {
            return Vector2.zero;
        }

        if (_gridVersion != _grid.Version)
        {
            ClearPath();
            _gridVersion = _grid.Version;
        }

        if (Vector2.Distance(position, destination) <= Mathf.Max(0f, goal.StoppingDistance))
        {
            ClearPath();
            return Vector2.zero;
        }

        if (Time.time >= _nextRepathTime)
        {
            _destination = destination;
            _pathfinder.TryFindPath(_grid, position, _destination, _path, CanReachWaypoint);
            _waypointIndex = 0;
            // 여러 몬스터가 같은 프레임에 반복 탐색하지 않도록 간격을 분산한다.
            float stagger = (GetInstanceID() & 255) / 255f * 0.1f;
            _nextRepathTime = Time.time + Mathf.Max(0.05f, _repathInterval) + stagger;
        }

        while (_waypointIndex < _path.Count && Vector2.Distance(position, _path[_waypointIndex]) <= 0.001f)
        {
            _waypointIndex++;
        }

        if (_waypointIndex >= _path.Count)
        {
            return Vector2.zero;
        }

        // 통과 가능한 앞쪽 경유 지점을 선택해 재탐색 때 뒤쪽 칸 중심으로 돌아가지 않는다.
        while (_waypointIndex + 1 < _path.Count && _grid.CanMoveDirectly(position, _path[_waypointIndex + 1]))
        {
            _waypointIndex++;
        }

        Vector2 next = _path[_waypointIndex];
        if (!CanReachWaypoint(next))
        {
            _path.Clear();
            _waypointIndex = 0;
            return Vector2.zero;
        }

        Vector2 delta = next - position;
        float distance = delta.magnitude;
        if (_waypointIndex == _path.Count - 1 && Vector2.Distance(_destination, destination) <= 0.001f)
        {
            distance = Mathf.Max(0f, distance - Mathf.Max(0f, goal.StoppingDistance));
        }

        return delta.normalized * Mathf.Min(moveSpeed, distance / Time.fixedDeltaTime);
    }

    private bool CanReachWaypoint(Vector2 next)
    {
        Vector2 position = (_body != null ? _body.position : (Vector2)transform.position) + _navigationOffset;
        if (_grid.CanMoveDirectly(position, next)) return true;

        // 벽 근처에서 안전 원만 장애물에 겹치면 실제 몸체가 통과할 수 있는 출구를 검사한다.
        if (_grid.IsPositionClear(position) || !_grid.IsPositionClear(next)) return false;
        Vector2 delta = next - position;
        if (delta.sqrMagnitude <= 0.00000001f) return false;
        Vector2 direction = delta.normalized;
        bool hasBody = false;
        foreach (Collider2D collider in _colliders)
        {
            if (!collider.enabled || collider.isTrigger || collider.attachedRigidbody != _body) continue;
            hasBody = true;
            int count = collider.Cast(direction, _grid.ObstacleFilter, _escapeHits, delta.magnitude);
            if (count == _escapeHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _escapeHits[i];
                // 이미 닿은 벽에서는 벽에서 멀어지는 이동만 허용한다.
                if (hit.distance <= 0.001f && Vector2.Dot(direction, hit.normal) > 0.001f) continue;
                return false;
            }
        }

        return hasBody;
    }

    private bool ResolveGrid(Vector2 position)
    {
        // 프리팹 에셋은 Start가 실행되지 않으므로 런타임 격자로 사용할 수 없다.
        if (_grid != null && !_grid.gameObject.scene.IsValid())
            _grid = null;

        if (_grid == null)
        {
            // 실패 시 매 프레임 검색하지 않고 재시도 간격을 둔다.
            if (Time.time < _nextRepathTime)
            {
                return false;
            }

            foreach (EnemyNavigationGrid candidate in FindObjectsByType<EnemyNavigationGrid>(FindObjectsSortMode.None))
            {
                if (!candidate.isActiveAndEnabled || !candidate.IsBuilt || candidate.AgentRadius < _requiredRadius ||
                    !candidate.TryWorldToCell(position, out _))
                {
                    continue;
                }

                if (_grid == null || candidate.AgentRadius < _grid.AgentRadius) _grid = candidate;
            }
        }

        if (_grid != null && _grid.isActiveAndEnabled && _grid.IsBuilt && _grid.AgentRadius >= _requiredRadius)
        {
            _warnedMissingGrid = false;
            return true;
        }

        if (!_warnedMissingGrid)
        {
            float minimumRadius = Mathf.Ceil(_requiredRadius * 1000f) / 1000f;
            Debug.LogWarning(
                $"EnemyNavigation ({name}): 사용 가능한 Grid가 없어 이동할 수 없습니다. " +
                $"EnemyNavigationGrid의 Agent Radius를 최소 {minimumRadius:F3} 이상으로 설정하고, " +
                "Enemy 위치를 포함하는 Grid를 활성화한 뒤 Build Grid를 실행하세요.", this);
            _warnedMissingGrid = true;
        }

        _nextRepathTime = Time.time + 1f;
        return false;
    }

    public void ClearPath()
    {
        _path.Clear();
        _waypointIndex = 0;
        _nextRepathTime = 0f;
    }

    private void OnDisable() => ClearPath();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 previous = transform.position + (Vector3)_navigationOffset;
        for (int i = _waypointIndex; i < _path.Count; i++)
        {
            Vector3 next = new Vector3(_path[i].x, _path[i].y, previous.z);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
