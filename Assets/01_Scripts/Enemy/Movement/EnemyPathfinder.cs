using System;
using System.Collections.Generic;
using UnityEngine;

/// 요청별 탐색 상태를 사용하는 8방향 A*. Unity 물리 조회 때문에 메인 스레드에서 호출한다.
public class EnemyPathfinder
{
    private struct Entry
    {
        public Vector2Int Cell;
        public float G, F;
        public int Sequence;
    }

    public bool TryFindPath(EnemyNavigationGrid grid, Vector2 start, Vector2 destination, List<Vector2> result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        result.Clear();
        if (grid == null || !grid.IsBuilt || !grid.TryWorldToCell(start, out Vector2Int first) ||
            !grid.TryWorldToCell(destination, out Vector2Int last) || !grid.IsPositionClear(destination))
        {
            return false;
        }

        if (grid.CanMoveDirectly(start, destination))
        {
            result.Add(destination);
            return true;
        }

        if (!grid.IsWalkable(first))
        {
            first = FindNearestWalkableCell(grid, first);
        }

        if (!grid.IsWalkable(first) || !grid.IsWalkable(last) ||
            !grid.CanMoveDirectly(grid.CellToWorld(last), destination)) return false;

        var open = new SortedSet<Entry>(Comparer<Entry>.Create((a, b) =>
        {
            int order = a.F.CompareTo(b.F);
            return order != 0 ? order : a.Sequence.CompareTo(b.Sequence);
        }));
        var costs = new Dictionary<Vector2Int, float>();
        var parents = new Dictionary<Vector2Int, Vector2Int>();
        var closed = new HashSet<Vector2Int>();
        int sequence = 0;
        costs[first] = 0;
        open.Add(new Entry { Cell = first, F = Heuristic(first, last), Sequence = sequence++ });
        while (open.Count > 0)
        {
            Entry current = open.Min;
            open.Remove(current);
            if (!closed.Add(current.Cell)) continue;
            if (current.Cell == last)
            {
                Vector2Int cell = last;
                result.Add(grid.CellToWorld(cell));
                while (cell != first)
                {
                    cell = parents[cell];
                    result.Add(grid.CellToWorld(cell));
                }

                result.Reverse();
                result.Add(destination);
                return true;
            }

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var next = current.Cell + new Vector2Int(dx, dy);
                if (closed.Contains(next) || !grid.CanTraverse(current.Cell, next)) continue;
                float cost = current.G + (dx != 0 && dy != 0 ? 1.41421356f : 1f);
                if (costs.TryGetValue(next, out float known) && cost >= known) continue;
                costs[next] = cost;
                parents[next] = current.Cell;
                open.Add(new Entry { Cell = next, G = cost, F = cost + Heuristic(next, last), Sequence = sequence++ });
            }
        }

        return false;
    }

    // 주변 8칸을 검사해 가장 가까운 걸어갈 수 있는(초록색) 칸을 찾아 반환
    private Vector2Int FindNearestWalkableCell(EnemyNavigationGrid grid, Vector2Int center)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector2Int neighbor = center + new Vector2Int(dx, dy);
                if (grid.IsWalkable(neighbor))
                {
                    return neighbor; // 주변 초록색 타일 발견 시 탈출구로 지정
                }
            }
        }

        return center; // 주변도 다 막혀있다면 원본 반환
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int x = Mathf.Abs(a.x - b.x), y = Mathf.Abs(a.y - b.y);
        return Mathf.Max(x, y) + 0.41421356f * Mathf.Min(x, y);
    }
}