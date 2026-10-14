using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 층별 등장 원소 조합. 한 층 안에서는 같은 조합을 유지한다.
/// - 일반 층: 원소 1종 (랜덤)
/// - 특수 층: 3층 2종, 5층 3종, 7층 4종, 9층 4종 (서로 다른 종류, 랜덤 조합)
/// - 후보가 모자라면 있는 종류를 전부 쓴다.
/// </summary>
public static class StageElementComposition
{
    private static readonly Dictionary<int, int> SpecialFloorCounts = new()
    {
        { 3, 2 },
        { 5, 3 },
        { 7, 4 },
        { 9, 4 },
    };

    public static int GetElementCount(int depth)
    {
        return SpecialFloorCounts.TryGetValue(depth, out int count) ? count : 1;
    }

    /// <summary>이번 층에 쓸 원소 집합을 뽑는다. 비어 있으면 필터 없음(null 취급).</summary>
    public static HashSet<EnemyElementType> Resolve(int depth, IReadOnlyList<EnemyElementType> available)
    {
        var distinct = available
            .Where(e => e != EnemyElementType.None)
            .Distinct()
            .ToList();
        if (distinct.Count == 0) return null;

        int count = Mathf.Min(GetElementCount(depth), distinct.Count);
        var picked = new HashSet<EnemyElementType>();
        while (picked.Count < count && distinct.Count > 0)
        {
            int index = Random.Range(0, distinct.Count);
            picked.Add(distinct[index]);
            distinct.RemoveAt(index);
        }
        return picked;
    }
}
