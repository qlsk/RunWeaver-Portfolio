using System;
using UnityEngine;

public enum MissionType
{
    EnemyKill,       // 1. 적 처치
    SoulCollect,     // 2. 영혼 수집
    AltarDefense,    // 3. 제단 디펜스
    Survival,        // 4. 단순 생존
    CaptureZone      // 5. 거점 점령
}

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("미션 유형 선택")]
    [Tooltip("테스트하거나 진행할 미션 유형을 선택합니다.")]
    [SerializeField] private MissionType activeMissionType = MissionType.EnemyKill;

    [Header("현재 활성화된 미션 전략 (자동 할당)")]
    [SerializeField] private MissionBase currentMission;

    public event Action<float, string> OnMissionProgressChanged;

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
        SetupActiveMission();
    }

    private void Update()
    {
        if (currentMission != null && !currentMission.IsCompleted && !currentMission.IsFailed)
        {
            currentMission.UpdateMission(Time.deltaTime);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeCurrentMission();
    }

    private void UnsubscribeCurrentMission()
    {
        if (currentMission != null)
        {
            currentMission.OnMissionCompleted -= HandleMissionCompleted;
            currentMission.OnMissionFailed -= HandleMissionFailed;
            currentMission.OnMissionProgressUpdated -= HandleMissionProgressUpdated;
        }
    }

    /// <summary>
    /// 지정된 activeMissionType에 맞는 MissionBase 구현체를 준비하고 초기화합니다.
    /// </summary>
    public void SetupActiveMission()
    {
        UnsubscribeCurrentMission();

        if (currentMission == null)
        {
            var existingMissions = GetComponents<MissionBase>();
            foreach (var m in existingMissions)
            {
                if (IsMatchingType(m, activeMissionType))
                {
                    currentMission = m;
                    break;
                }
            }

            if (currentMission == null)
            {
                currentMission = CreateMissionComponent(activeMissionType);
            }
        }

        if (currentMission != null)
        {
            currentMission.OnMissionCompleted += HandleMissionCompleted;
            currentMission.OnMissionFailed += HandleMissionFailed;
            currentMission.OnMissionProgressUpdated += HandleMissionProgressUpdated;

            currentMission.Initialize();
            Debug.Log($"[MissionManager] 미션 활성화: {currentMission.GetType().Name} -> {currentMission.GetProgressText()}");
        }
        else
        {
            Debug.LogError("[MissionManager] 활성화할 미션 전략(MissionBase)을 생성하지 못했습니다.");
        }
    }

    private bool IsMatchingType(MissionBase mission, MissionType type)
    {
        return type switch
        {
            MissionType.EnemyKill => mission is EnemyKillMission,
            MissionType.SoulCollect => mission is SoulCollectMission,
            MissionType.AltarDefense => mission is AltarDefenseMission,
            MissionType.Survival => mission is SurvivalMission,
            MissionType.CaptureZone => mission is CaptureZoneMission,
            _ => false
        };
    }

    private MissionBase CreateMissionComponent(MissionType type)
    {
        return type switch
        {
            MissionType.EnemyKill => gameObject.AddComponent<EnemyKillMission>(),
            MissionType.SoulCollect => gameObject.AddComponent<SoulCollectMission>(),
            MissionType.AltarDefense => gameObject.AddComponent<AltarDefenseMission>(),
            MissionType.Survival => gameObject.AddComponent<SurvivalMission>(),
            MissionType.CaptureZone => gameObject.AddComponent<CaptureZoneMission>(),
            _ => gameObject.AddComponent<EnemyKillMission>()
        };
    }

    private void HandleMissionCompleted()
    {
        Debug.Log("★ [MissionManager] 임무 완수! 스테이지 클리어 조건을 달성했습니다. ★");

        if (StageManager.Instance != null)
        {
            StageManager.Instance.SpawnPortal();
        }
        else
        {
            Debug.LogWarning("[MissionManager] 씬에 StageManager가 존재하지 않습니다.");
        }
    }

    private void HandleMissionFailed()
    {
        Debug.LogWarning("✖ [MissionManager] 임무 실패! 재도전이 필요합니다. ✖");
    }

    private void HandleMissionProgressUpdated(float progressRatio, string progressText)
    {
        OnMissionProgressChanged?.Invoke(progressRatio, progressText);
    }

    public MissionBase GetCurrentMission() => currentMission;
}
