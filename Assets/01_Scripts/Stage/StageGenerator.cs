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
            depth = depth,
            maxActiveMonsters = _config.maxActiveMonsters,
            initialSpawnCount = _config.initialSpawnCount,
            spawnInterval = _config.spawnInterval,
            spawnBatchCount = _config.spawnBatchCount
        };

        // 1. 특수 몬스터 비율 계산 (심도당 스케일링)
        float calculatedSpecialRatio = _config.baseSpecialRatio + (depth - 1) * _config.specialRatioPerDepth;
        runtimeData.specialMonsterRatio = Mathf.Min(calculatedSpecialRatio, _config.maxSpecialRatio);

        // 2. 고정 일반 몬스터 필수 등록
        if (_config.fixedNormalMonster != null)
        {
            runtimeData.selectedMonsters.Add(_config.fixedNormalMonster);
        }
        else
        {
            Debug.LogWarning("[StageGenerator] fixedNormalMonster가 설정되지 않았습니다!");
        }

        // 3. 심도별 목표 몬스터 종수 계산 (1 ~ 3종)
        int targetTotalTypeCount = 1;
        if (depth >= _config.depthForThreeTypes) targetTotalTypeCount = 3;
        else if (depth >= _config.depthForTwoTypes) targetTotalTypeCount = 2;

        // 4. 추가 몬스터 풀에서 출현 가능한 후보 추출 (최소 심도 만족 & 고정 몹 제외)
        var availableAdditionalPool = _config.additionalMonsterPool
            .Where(m => m != null && m.minSpawnDepth <= depth && !runtimeData.selectedMonsters.Contains(m))
            .ToList();

        // 5. 목표 종수가 될 때까지 추가 풀에서 무작위 선택
        while (runtimeData.selectedMonsters.Count < targetTotalTypeCount && availableAdditionalPool.Count > 0)
        {
            int randomIndex = Random.Range(0, availableAdditionalPool.Count);
            MonsterDataSO picked = availableAdditionalPool[randomIndex];
            runtimeData.selectedMonsters.Add(picked);
            availableAdditionalPool.RemoveAt(randomIndex);
        }

        // 6. 몬스터별 최종 스폰 비율 산출
        CalculateSpawnRatios(runtimeData);

        Debug.Log($"[StageGenerator] Depth {depth} 데이터 생성 완료 | 최대 마릿수: {runtimeData.maxActiveMonsters} | 초기 스폰: {runtimeData.initialSpawnCount} | {runtimeData.spawnInterval}초당 {runtimeData.spawnBatchCount}마리 | 특수비율: {runtimeData.specialMonsterRatio * 100:F0}% | 등장 종수: {runtimeData.selectedMonsters.Count}");

        return runtimeData;
    }

    private void CalculateSpawnRatios(StageRuntimeData data)
    {
        data.monsterSpawnRatios.Clear();

        int normalCount = data.selectedMonsters.Count(m => m.category == MonsterCategory.Normal);
        int specialCount = data.selectedMonsters.Count(m => m.category == MonsterCategory.Special);

        float normalRatioTotal = 1f - data.specialMonsterRatio;
        float specialRatioTotal = data.specialMonsterRatio;

        // 특수 몬스터가 한 종도 없다면 일반 몬스터가 100% 비중 차지
        if (specialCount == 0)
        {
            normalRatioTotal = 1f;
            specialRatioTotal = 0f;
        }

        foreach (var monster in data.selectedMonsters)
        {
            if (monster.category == MonsterCategory.Normal)
            {
                data.monsterSpawnRatios.Add(normalRatioTotal / Mathf.Max(1, normalCount));
            }
            else
            {
                data.monsterSpawnRatios.Add(specialRatioTotal / Mathf.Max(1, specialCount));
            }
        }
    }
}