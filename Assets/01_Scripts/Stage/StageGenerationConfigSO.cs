using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "StageGenConfigSO", menuName = "Stage/Generation ConfigSO")]
public class StageGenerationConfigSO : ScriptableObject
{
    [Header("1. 스폰 규칙 기본값 (1층부터 적용)")]
    [Tooltip("맵 상에 동시에 존재할 수 있는 최대 몬스터 수")]
    [FormerlySerializedAs("maxActiveMonsters")] public int MaxActiveMonsters = 30;

    [Tooltip("스테이지 시작 시 즉시 생성되는 초기 스폰 수")]
    [FormerlySerializedAs("initialSpawnCount")] public int InitialSpawnCount = 8;

    [Tooltip("몬스터 추가 스폰 주기 (초)")]
    [FormerlySerializedAs("spawnInterval")] public float SpawnInterval = 5f;

    [Tooltip("스폰 주기마다 동시 생성되는 몬스터 수")]
    [FormerlySerializedAs("spawnBatchCount")] public int SpawnBatchCount = 5;

    [Header("2. 특수 몬스터 비율(Special Ratio) 스케일링")]
    [Range(0f, 1f), FormerlySerializedAs("baseSpecialRatio")] public float BaseSpecialRatio = 0.1f;      // 1층 기본 특수 몹 비율 (10%)
    [Range(0f, 1f), FormerlySerializedAs("specialRatioPerDepth")] public float SpecialRatioPerDepth = 0.05f;   // 심도당 비율 증가량 (5%)
    [Range(0f, 1f), FormerlySerializedAs("maxSpecialRatio")] public float MaxSpecialRatio = 0.6f;       // 특수 몹 최대 비율 제한 (60%)

    [Header("3. 심도별 몬스터 가짓수 규칙 (최대 3종)")]
    [FormerlySerializedAs("depthForTwoTypes")] public int DepthForTwoTypes = 3;   // 심도 3 이상부터 2종 (고정 1종 + 추가 1종)
    [FormerlySerializedAs("depthForThreeTypes")] public int DepthForThreeTypes = 6; // 심도 6 이상부터 3종 (고정 1종 + 추가 2종)

    [Header("4. 고정 일반 몬스터 및 추가 몬스터 풀")]
    [Tooltip("모든 심도에서 항상 1종 고정으로 등장하는 일반 몬스터")]
    [FormerlySerializedAs("fixedNormalMonster")] public MonsterDataSO FixedNormalMonster;

    [Tooltip("심도 조건에 따라 추가로 선택되는 특수/일반 몬스터 풀")]
    [FormerlySerializedAs("additionalMonsterPool")] public List<MonsterDataSO> AdditionalMonsterPool = new List<MonsterDataSO>();
}
