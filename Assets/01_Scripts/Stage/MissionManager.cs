using System;
using System.Collections.Generic;
using UnityEngine;

public enum MissionType
{
    EnemyKill,     // 1. 적 처치
    SoulCollect,   // 2. 영혼 수집 (구역 필요)
    AltarDefense,  // 3. 제단 방어 (제단 필요)
    Survival,      // 4. 제한시간 생존
    CaptureZone    // 5. 거점 점령 (거점 필요)
}

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("미션 전용 오브젝트 프리팹")]
    [SerializeField] private GameObject captureZonePrefab;     // 거점 점령용 프리팹
    [SerializeField] private GameObject defenseAltarPrefab;    // 제단 방어용 프리팹
    [SerializeField] private GameObject soulHarvestZonePrefab; // 영혼 수집 구역용 프리팹

    [Header("스폰 위치 설정")]
    [SerializeField] private Vector3 missionObjectSpawnPosition = Vector3.zero;

    private MissionBase currentMission;
    private GameObject spawnedMissionObject;

    public event Action<MissionBase> OnMissionInitialized;
    // 현재 미션이 클리어되었을 때 (몬스터 스폰 중지 등에 사용)
    public event Action<MissionBase> OnMissionCompleted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (currentMission == null && StageManager.Instance != null)
        {
            StartRandomMission(StageManager.Instance.CurrentDepth);
        }
    }

    /// <summary>
    /// 새로운 층수에 진입할 때 무작위 미션을 추첨하고 해당 오브젝트를 스폰합니다.
    /// </summary>
    public void StartRandomMission(int depth)
    {
        CleanupCurrentMission();

        Array missionValues = Enum.GetValues(typeof(MissionType));
        MissionType randomType = (MissionType)missionValues.GetValue(UnityEngine.Random.Range(0, missionValues.Length));

        Debug.Log($"[MissionManager] ===== Depth {depth} 무작위 미션 추첨: {randomType} =====");

        SetupMissionStrategy(randomType);

        if (currentMission != null)
        {
            currentMission.Initialize();
            currentMission.OnMissionCompleted -= HandleMissionCompleted;
            currentMission.OnMissionCompleted += HandleMissionCompleted;
            OnMissionInitialized?.Invoke(currentMission);
        }
    }

    private void SetupMissionStrategy(MissionType type)
    {
        // 기존에 부착되어 있던 미션 전략 컴포넌트 정리
        MissionBase[] oldMissions = GetComponents<MissionBase>();
        foreach (var old in oldMissions)
        {
            Destroy(old);
        }

        switch (type)
        {
            case MissionType.EnemyKill:
                currentMission = gameObject.AddComponent<EnemyKillMission>();
                break;

            case MissionType.SoulCollect:
                if (soulHarvestZonePrefab != null)
                {
                    spawnedMissionObject = Instantiate(soulHarvestZonePrefab, missionObjectSpawnPosition, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("[MissionManager] soulHarvestZonePrefab이 연결되지 않았습니다!");
                }
                currentMission = gameObject.AddComponent<SoulCollectMission>();
                break;

            case MissionType.AltarDefense:
                DefenseAltar spawnedAltar = null;
                if (defenseAltarPrefab != null)
                {
                    spawnedMissionObject = Instantiate(defenseAltarPrefab, missionObjectSpawnPosition, Quaternion.identity);
                    spawnedAltar = spawnedMissionObject.GetComponent<DefenseAltar>();
                }
                else
                {
                    Debug.LogWarning("[MissionManager] defenseAltarPrefab이 연결되지 않았습니다!");
                }

                var altarMission = gameObject.AddComponent<AltarDefenseMission>();
                if (spawnedAltar != null)
                {
                    altarMission.SetTargetAltar(spawnedAltar); // 생성된 신규 제단 직접 주입
                }
                currentMission = altarMission;
                break;

            case MissionType.Survival:
                currentMission = gameObject.AddComponent<SurvivalMission>();
                break;

            case MissionType.CaptureZone:
                if (captureZonePrefab != null)
                {
                    spawnedMissionObject = Instantiate(captureZonePrefab, missionObjectSpawnPosition, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("[MissionManager] captureZonePrefab이 연결되지 않았습니다!");
                }
                currentMission = gameObject.AddComponent<CaptureZoneMission>();
                break;
        }
    }

    private void Update()
    {
        if (currentMission != null && !currentMission.IsCompleted && !currentMission.IsFailed)
        {
            currentMission.UpdateMission(Time.deltaTime);
        }
    }

    private void HandleMissionCompleted()
    {
        Debug.Log("[MissionManager] ★ 미션 달성! StageManager에 포탈 생성 요청");
        OnMissionCompleted?.Invoke(currentMission);
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SpawnPortalAtCenter();
        }
    }

    public void CleanupCurrentMission()
    {
        if (currentMission != null)
        {
            currentMission.OnMissionCompleted -= HandleMissionCompleted;
            Destroy(currentMission);
            currentMission = null;
        }

        if (spawnedMissionObject != null)
        {
            Destroy(spawnedMissionObject);
            spawnedMissionObject = null;
        }
    }

    public MissionBase GetCurrentMission() => currentMission;
    
    /// <summary>
    /// 포탈 생성 시점에 필드의 미션 오브젝트(제단, 점령구역 등)만 제거합니다.
    /// </summary>
    public void CleanupMissionObject()
    {
        if (spawnedMissionObject != null)
        {
            Destroy(spawnedMissionObject);
            spawnedMissionObject = null;
            Debug.Log("[MissionManager] 미션 오브젝트 제거 완료");
        }
    }
}