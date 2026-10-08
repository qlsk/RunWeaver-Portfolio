using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class StageGenerator : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("config")] private StageGenerationConfigSO _config;

    /// <summary>
    /// 전달받은 심도(depth) 기반으로 StageRuntimeData를 계산해 반환
    /// </summary>
    public StageRuntimeData GenerateStageData(int depth)
    {
        if (_config == null)
        {
            Debug.LogError("[StageGenerator] StageGenerationConfigSO가 연결되지 않았습니다!");
            return null;
        }

        StageRuntimeData runtimeData = new StageRuntimeData
        {
            Depth = depth,
            MaxActiveMonsters = _config.MaxActiveMonsters,
            InitialSpawnCount = _config.InitialSpawnCount,
            SpawnInterval = _config.SpawnInterval,
            SpawnBatchCount = _config.SpawnBatchCount
        };

        // 1. 특수 몬스터 비율 계산 (심도당 스케일링)
        float calculatedSpecialRatio = _config.BaseSpecialRatio + (depth - 1) * _config.SpecialRatioPerDepth;
        runtimeData.SpecialMonsterRatio = Mathf.Min(calculatedSpecialRatio, _config.MaxSpecialRatio);

        // 2. 고정 일반 몬스터 필수 등록
        if (_config.FixedNormalMonster != null)
        {
            runtimeData.SelectedMonsters.Add(_config.FixedNormalMonster);
        }
        else
        {
            Debug.LogWarning("[StageGenerator] fixedNormalMonster가 설정되지 않았습니다!");
        }

        // 3. 심도별 목표 몬스터 종수 계산 (1 ~ 3종)
        int targetTotalTypeCount = 1;
        if (depth >= _config.DepthForThreeTypes) targetTotalTypeCount = 3;
        else if (depth >= _config.DepthForTwoTypes) targetTotalTypeCount = 2;

        // 4. 추가 몬스터 풀에서 출현 가능한 후보 추출 (최소 심도 만족 & 고정 몹 제외)
        var availableAdditionalPool = _config.AdditionalMonsterPool
            .Where(m => m != null && m.MinSpawnDepth <= depth && !runtimeData.SelectedMonsters.Contains(m))
            .ToList();

        // 5. 목표 종수가 될 때까지 추가 풀에서 무작위 선택
        while (runtimeData.SelectedMonsters.Count < targetTotalTypeCount && availableAdditionalPool.Count > 0)
        {
            int randomIndex = Random.Range(0, availableAdditionalPool.Count);
            MonsterDataSO picked = availableAdditionalPool[randomIndex];
            runtimeData.SelectedMonsters.Add(picked);
            availableAdditionalPool.RemoveAt(randomIndex);
        }

        // 6. 몬스터별 최종 스폰 비율 산출
        CalculateSpawnRatios(runtimeData);

        Debug.Log($"[StageGenerator] Depth {depth} 데이터 생성 완료 | 최대 마릿수: {runtimeData.MaxActiveMonsters} | 초기 스폰: {runtimeData.InitialSpawnCount} | {runtimeData.SpawnInterval}초당 {runtimeData.SpawnBatchCount}마리 | 특수비율: {runtimeData.SpecialMonsterRatio * 100:F0}% | 등장 종수: {runtimeData.SelectedMonsters.Count}");

        return runtimeData;
    }

    private void CalculateSpawnRatios(StageRuntimeData data)
    {
        data.MonsterSpawnRatios.Clear();

        int normalCount = data.SelectedMonsters.Count(m => m.Category == MonsterCategory.Normal);
        int specialCount = data.SelectedMonsters.Count(m => m.Category == MonsterCategory.Special);

        float normalRatioTotal = 1f - data.SpecialMonsterRatio;
        float specialRatioTotal = data.SpecialMonsterRatio;

        // 특수 몬스터가 한 종도 없다면 일반 몬스터가 100% 비중 차지
        if (specialCount == 0)
        {
            normalRatioTotal = 1f;
            specialRatioTotal = 0f;
        }

        foreach (var monster in data.SelectedMonsters)
        {
            if (monster.Category == MonsterCategory.Normal)
            {
                data.MonsterSpawnRatios.Add(normalRatioTotal / Mathf.Max(1, normalCount));
            }
            else
            {
                data.MonsterSpawnRatios.Add(specialRatioTotal / Mathf.Max(1, specialCount));
            }
        }
    }
}
