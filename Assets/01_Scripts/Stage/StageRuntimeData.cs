using System.Collections.Generic;

public class StageRuntimeData
{
    public int Depth;
    public int MaxActiveMonsters;      // 맵 내 최대 존재 가능 마릿수 (30)
    public int InitialSpawnCount;     // 시작 시 초기 스폰 수 (8)
    public float SpawnInterval;       // 스폰 주기 (5초)
    public int SpawnBatchCount;       // 회당 동시 스폰 수 (5마리)
    public float SpecialMonsterRatio;  // 특수 몬스터 비율 (0.0 ~ 1.0)

    // 최종 선택된 몬스터 조합 (고정 1종 포함 최대 3종)
    public List<MonsterDataSO> SelectedMonsters = new List<MonsterDataSO>();

    // 각 선택된 몬스터의 최종 스폰 가중치/비율 (합계 1.0)
    public List<float> MonsterSpawnRatios = new List<float>();
}
