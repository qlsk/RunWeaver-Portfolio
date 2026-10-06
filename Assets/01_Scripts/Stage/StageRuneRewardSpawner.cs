using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미션 클리어 시 랜덤 룬석을 픽업 형태로 스폰하는 스테이지 보상 스포너.
///
/// - EnemyDrop.RuneDrop과 같은 방식(EnemyDrop 수정 없이 재사용):
///   RuneStoneDatabase.GetAll()에서 랜덤 추첨 → RunePickup 프리팹 Instantiate → Initialize()
/// - 지급 시점: MissionManager.OnMissionCompleted (미션 클리어 직후, 포탈 등장 전)
/// - 지급 개수: 심도 비례. base + ((depth-1) / interval) * bonus, 최대 max로 클램프.
///   예) base=1, interval=5, bonus=1 → 1~5층 1개, 6~10층 2개, 11~15층 3개 ...
/// - 스폰 위치: 플레이어 주변 원형 랜덤. Player 태그를 못 찾으면 원점 기준.
/// </summary>
[DisallowMultipleComponent]
public class StageRuneRewardSpawner : MonoBehaviour
{
    [Header("룬 픽업 프리팹")]
    [Tooltip("EnemyDrop에서 쓰는 것과 같은 RunePickup 프리팹 (Assets/02_Prefabs/Rune/Rune.prefab)")]
    [SerializeField] private RunePickup _runePickupPrefab;

    [Header("심도 비례 지급")]
    [Tooltip("기본 지급 개수 (1층 구간)")]
    [SerializeField, Min(0)] private int _baseRewardCount = 1;
    [Tooltip("이 층수 단위마다 보너스가 붙는다 (5면 6층부터 +bonus)")]
    [SerializeField, Min(1)] private int _intervalDepth = 5;
    [Tooltip("구간마다 추가되는 개수")]
    [SerializeField, Min(0)] private int _bonusPerInterval = 1;
    [Tooltip("한 번에 스폰되는 최대 개수")]
    [SerializeField, Min(1)] private int _maxRewardCount = 4;

    [Header("스폰")]
    [Tooltip("픽업 1개당 들어있는 룬석 수량")]
    [SerializeField, Min(1)] private int _quantityPerPickup = 1;
    [Tooltip("플레이어 기준 스폰 반경")]
    [SerializeField, Min(0f)] private float _spawnRadius = 2f;

    private MissionManager _missionManager;

    private void Start()
    {
        _missionManager = MissionManager.Instance;
        if (_missionManager != null)
        {
            _missionManager.OnMissionCompleted -= HandleMissionCompleted;
            _missionManager.OnMissionCompleted += HandleMissionCompleted;
        }
        else
        {
            Debug.LogWarning("[StageRuneRewardSpawner] MissionManager를 찾지 못해 보상을 지급할 수 없습니다.", this);
        }
    }

    private void OnDestroy()
    {
        if (_missionManager != null)
        {
            _missionManager.OnMissionCompleted -= HandleMissionCompleted;
        }
    }

    private void HandleMissionCompleted(MissionBase mission)
    {
        int depth = StageManager.Instance != null ? StageManager.Instance.CurrentDepth : 1;
        int count = GetRewardCount(depth);
        SpawnRewards(count, depth);
    }

    /// <summary>
    /// 심도에 따른 보상 개수를 계산한다.
    /// </summary>
    public int GetRewardCount(int depth)
    {
        int safeDepth = Mathf.Max(1, depth);
        int safeInterval = Mathf.Max(1, _intervalDepth);
        int count = _baseRewardCount + ((safeDepth - 1) / safeInterval) * _bonusPerInterval;
        return Mathf.Clamp(count, 0, Mathf.Max(1, _maxRewardCount));
    }

    private void SpawnRewards(int count, int depth)
    {
        if (count <= 0)
        {
            return;
        }

        if (_runePickupPrefab == null)
        {
            Debug.LogWarning("[StageRuneRewardSpawner] RunePickup 프리팹이 비어 있어 보상을 스폰하지 못했습니다.", this);
            return;
        }

        RuneStoneDatabase database = RuneStoneDatabase.Instance;
        if (database == null || !database.DatabaseReady)
        {
            Debug.LogWarning("[StageRuneRewardSpawner] RuneStoneDatabase가 준비되지 않아 보상을 스폰하지 못했습니다.", this);
            return;
        }

        IReadOnlyList<RuneStoneDefinition> runeStones = database.GetAll();
        if (runeStones == null || runeStones.Count == 0)
        {
            Debug.LogWarning("[StageRuneRewardSpawner] 룬석 정의가 비어 있어 보상을 스폰하지 못했습니다.", this);
            return;
        }

        Vector3 center = GetSpawnCenter();

        for (int i = 0; i < count; i++)
        {
            RuneStoneDefinition def = runeStones[Random.Range(0, runeStones.Count)];
            Vector3 pos = center + (Vector3)Random.insideUnitCircle * _spawnRadius;
            RunePickup pickup = Instantiate(_runePickupPrefab, pos, Quaternion.identity);
            pickup.Initialize(def.RuneCharacter, Mathf.Max(1, _quantityPerPickup), def.Icon);
        }

        Debug.Log($"[StageRuneRewardSpawner] Depth {depth} 클리어 보상: 랜덤 룬석 {count}개 스폰");
    }

    private Vector3 GetSpawnCenter()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform.position;
        }

        return Vector3.zero;
    }
}
