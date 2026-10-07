using UnityEngine;
using UnityEngine.Serialization;

public enum MonsterCategory
{
    Normal,  // 일반 몬스터 (기본/근접)
    Special  // 특수 몬스터 (원거리, 돌진, 엘리트 등)
}

[CreateAssetMenu(fileName = "MonsterDataSO", menuName = "Stage/Monster DataSO")]
public class MonsterDataSO : ScriptableObject
{
    [Header("몬스터 기본 정보")]
    [FormerlySerializedAs("monsterName")] public string MonsterName = "New Monster";
    [FormerlySerializedAs("category")] public MonsterCategory Category = MonsterCategory.Normal;
    [FormerlySerializedAs("monsterPrefab")] public GameObject MonsterPrefab;

    [Header("스폰 제약조건")]
    [Tooltip("이 몬스터가 출현할 수 있는 최소 심도(층수)")]
    [FormerlySerializedAs("minSpawnDepth")] public int MinSpawnDepth = 1;

    [Tooltip("기본 가중치 (추첨 시 상대적 가중치)")]
    [FormerlySerializedAs("baseWeight")] public float BaseWeight = 10f;
}
