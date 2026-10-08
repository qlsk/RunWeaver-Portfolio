using System.Collections.Generic;
using UnityEngine;

/// 자신의 경로를 갱신하고 다음 경유 지점으로 향하는 희망 속도를 반환한다.
public class EnemyNavigation : MonoBehaviour
{
    [SerializeField] private EnemyNavigationGrid _grid;
    [SerializeField] private float _agentRadius = 0.3f;
    [SerializeField] private float _repathInterval = 0.4f;
    private readonly EnemyPathfinder _pathfinder = new EnemyPathfinder();
    private readonly List<Vector2> _path = new List<Vector2>();
    private int _waypointIndex, _gridVersion = -1;
    private float _nextRepathTime;
    private bool _warnedMissingGrid;
    private Vector2 _destination;
    private Rigidbody2D _body;
    private float _requiredRadius;

    private void Start()
    {
        _body = GetComponent<Rigidbody2D>();
        _requiredRadius = Mathf.Max(0.01f, _agentRadius);
        // 루트 이동 위치에서 충돌체의 가장 먼 모서리까지 보수적으로 감싼다.
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
        {
            if (!collider.enabled || collider.isTrigger || collider.attachedRigidbody != _body)
            {
                continue;
            }

            Bounds bounds = collider.bounds;
            Vector2 offset = (Vector2)bounds.center - (Vector2)transform.position;
            Vector2 extent = bounds.extents;
            _requiredRadius = Mathf.Max(_requiredRadius,
                new Vector2(Mathf.Abs(offset.x) + extent.x, Mathf.Abs(offset.y) + extent.y).magnitude);
        }
    }

    public Vector2 CalculateVelocity(MovementGoal goal, float moveSpeed)
    {
        if (!isActiveAndEnabled || goal.ShouldStop || moveSpeed <= 0f)
        {
            ClearPath();
            return Vector2.zero;
        }

        Vector2 position = _body != null ? _body.position : (Vector2)transform.position;
        if (!ResolveGrid(position))
        {
            return Vector2.zero;
        }

        if (_gridVersion != _grid.Version)
        {
            ClearPath();
            _gridVersion = _grid.Version;
        }

        if (Vector2.Distance(position, goal.Destination) <= Mathf.Max(0f, goal.StoppingDistance))
        {
            ClearPath();
            return Vector2.zero;
        }

        if (Time.time >= _nextRepathTime)
        {
            _destination = goal.Destination;
            _pathfinder.TryFindPath(_grid, position, _destination, _path);
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
        // if (!_grid.CanMoveDirectly(position, next))
        // {
        //     _path.Clear();
        //     return Vector2.zero;
        // }

        Vector2 delta = next - position;
        float distance = delta.magnitude;
        if (_waypointIndex == _path.Count - 1 && Vector2.Distance(_destination, goal.Destination) <= 0.001f)
        {
            distance = Mathf.Max(0f, distance - Mathf.Max(0f, goal.StoppingDistance));
        }

        return delta.normalized * Mathf.Min(moveSpeed, distance / Time.fixedDeltaTime);
    }

    private bool ResolveGrid(Vector2 position)
    {
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
            Debug.LogWarning("EnemyNavigation: 몬스터 반지름을 수용하는 구축된 Grid를 연결하세요.", this);
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
        Vector3 previous = transform.position;
        for (int i = _waypointIndex; i < _path.Count; i++)
        {
            Vector3 next = new Vector3(_path[i].x, _path[i].y, previous.z);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}