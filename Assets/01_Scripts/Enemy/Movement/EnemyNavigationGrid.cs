using System;
using UnityEngine;

/// 같은 몸 크기의 몬스터가 공유하는 월드 XY 격자. 변경된 장애물은 BuildGrid로 갱신한다.
public class EnemyNavigationGrid : MonoBehaviour
{
    [SerializeField] private Vector2 _worldSize = new Vector2(20f, 10f);
    [SerializeField] private float _cellSize = 0.5f;
    [SerializeField] private float _agentRadius = 0.3f;
    [SerializeField] private LayerMask _obstacleLayers;
    [SerializeField] private bool _drawCells = true;
    private Vector2 _origin;
    private int _width;
    private int _height;
    private bool[,] _walkable;
    private float _builtCellSize, _builtRadius;
    private ContactFilter2D _filter;
    private readonly Collider2D[] _overlaps = new Collider2D[1];
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[1];
    public int Width => _width;
    public int Height => _height;
    public bool IsBuilt => _walkable != null;
    public float AgentRadius => IsBuilt ? _builtRadius : _agentRadius;
    public int Version { get; private set; }

    private void Start()
    {
        if (!IsBuilt)
        {
            BuildGrid();
        }
    }

    [ContextMenu("Build Grid")]
    public void BuildGrid()
    {
        Version++;
        _walkable = null;
        _width = _height = 0;
        if (!Positive(_worldSize.x) || !Positive(_worldSize.y) || !Positive(_cellSize) ||
            !Positive(_agentRadius) || _obstacleLayers.value == 0)
        {
            Debug.LogWarning("EnemyNavigationGrid: 유효한 맵 크기, 칸 크기, 반지름과 장애물 레이어를 지정하세요.", this);
            return;
        }

        double w = Math.Ceiling((double)_worldSize.x / _cellSize);
        double h = Math.Ceiling((double)_worldSize.y / _cellSize);

        _width = (int)w;
        _height = (int)h;
        _builtCellSize = _cellSize;
        _builtRadius = _agentRadius;
        _origin = (Vector2)transform.position - new Vector2(_width, _height) * _builtCellSize * 0.5f;
        _filter = new ContactFilter2D();
        _filter.SetLayerMask(_obstacleLayers);
        _filter.useTriggers = false;
        _walkable = new bool[_width, _height];
        Physics2D.SyncTransforms();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _walkable[x, y] = IsPositionClear(CellToWorld(new Vector2Int(x, y)));
            }
        }
    }

    public bool TryWorldToCell(Vector2 position, out Vector2Int cell)
    {
        cell = default;
        if (!IsBuilt || !Finite(position.x) || !Finite(position.y))
        {
            return false;
        }

        Vector2 p = position - _origin;
        if (p.x < 0 || p.y < 0 || p.x >= _width * _builtCellSize || p.y >= _height * _builtCellSize)
        {
            return false;
        }

        cell = new Vector2Int(Mathf.FloorToInt(p.x / _builtCellSize), Mathf.FloorToInt(p.y / _builtCellSize));
        return true;
    }

    public Vector2 CellToWorld(Vector2Int cell)
    {
        if (!IsBuilt)
        {
            throw new InvalidOperationException("BuildGrid를 먼저 호출하세요.");
        }

        if (!Contains(cell))
        {
            throw new ArgumentOutOfRangeException(nameof(cell));
        }

        return _origin + new Vector2(cell.x + 0.5f, cell.y + 0.5f) * _builtCellSize;
    }

    public bool IsWalkable(Vector2Int cell) => IsBuilt && Contains(cell) && _walkable[cell.x, cell.y];

    public bool CanTraverse(Vector2Int from, Vector2Int to)
    {
        if (!IsWalkable(from) || !IsWalkable(to))
        {
            return false;
        }

        int dx = Mathf.Abs(to.x - from.x), dy = Mathf.Abs(to.y - from.y);
        if (dx > 1 || dy > 1 || dx + dy == 0)
        {
            return false;
        }

        if (dx == 1 && dy == 1 && (!IsWalkable(new Vector2Int(to.x, from.y)) ||
                                   !IsWalkable(new Vector2Int(from.x, to.y))))
        {
            return false;
        }

        return CanMoveDirectly(CellToWorld(from), CellToWorld(to));
    }

    /// 격자 중심이 아닌 실제 위치와 경유 지점 사이도 몸 크기로 검사한다.
    public bool CanMoveDirectly(Vector2 from, Vector2 to)
    {
        if (!IsPositionClear(from) || !IsPositionClear(to))
        {
            return false;
        }

        Vector2 delta = to - from;
        return delta.sqrMagnitude <= 0.00000001f ||
               Physics2D.CircleCast(from, _builtRadius, delta.normalized, _filter, _hits, delta.magnitude) == 0;
    }

    public bool IsPositionClear(Vector2 point)
    {
        return TryWorldToCell(point, out _) &&
               Physics2D.OverlapCircle(point, _builtRadius, _filter, _overlaps) == 0;
    }

    private bool Contains(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < _width && c.y < _height;
    private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    private static bool Positive(float v) => v > 0 && Finite(v);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (!IsBuilt)
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(_worldSize.x, _worldSize.y, 0));
            return;
        }

        if (!_drawCells) return;
        for (int x = 0; x < _width; x++)
        for (int y = 0; y < _height; y++)
        {
            Gizmos.color = _walkable[x, y] ? new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.4f);
            Vector2 p = CellToWorld(new Vector2Int(x, y));
            Gizmos.DrawCube(new Vector3(p.x, p.y, transform.position.z),
                new Vector3(_builtCellSize * 0.9f, _builtCellSize * 0.9f, 0.01f));
        }
    }
}