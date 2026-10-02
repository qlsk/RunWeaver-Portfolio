using UnityEngine;

[System.Serializable]
public class EnemyInfoData
{
    [SerializeField] private MonsterType _monsterType = MonsterType.normal;
    public MonsterType MonsterType => _monsterType;

    [SerializeField] private int _tier = 1;
    public int Tier => _tier;

    [SerializeField] private float _weight = 100f;
    public float Weight => _weight;

    [SerializeField] private int _minDepth = 1;
    public int MinDepth => _minDepth;

    [SerializeField] private int _cost = 1; // 스폰 예산 차감 포인트 (1티어: 1pt, 3티어: 8pt 등)
    public int Cost => _cost;

    /// <summary>
    /// 현재 Depth 및 티어 성장 계수(α)를 반영하여 출현 가중치를 계산합니다.
    /// 공식: BaseWeight + (Depth - MinDepth) * (Tier * α)
    /// </summary>
    public float GetCalculatedWeight(int currentDepth, float tierGrowthMultiplier = 5f)
    {
        if (currentDepth < _minDepth) return 0f;

        int depthDiff = currentDepth - _minDepth;
        float addedWeight = depthDiff * (_tier * tierGrowthMultiplier);

        return _weight + addedWeight;
    }
}