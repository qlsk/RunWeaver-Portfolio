using UnityEngine;

[System.Serializable]
public class EnemyInfoData
{
    [SerializeField] private MonsterType _monsterType;
    public MonsterType MonsterType => _monsterType;

    [SerializeField] private int _tier;
    public int Tier => _tier;

    [SerializeField] private float _weight;
    public float Weight => _weight;

    [SerializeField] private int _minDepth;
    public int MinDepth => _minDepth;
}