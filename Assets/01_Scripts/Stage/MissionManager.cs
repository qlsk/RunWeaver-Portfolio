using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum MissionType
{
    EnemyKill,   // 1. 적 처치
    SoulCollect, // 2. 영혼 수집 (구역 필요)
    AltarDefense,// 3. 제단 방어 (제단 필요)
    Survival,    // 4. 제한시간 생존
    CaptureZone  // 5. 거점 점령 (거점 필요)
}

public class MissionManager : MonoBehaviour
{
    private static MissionManager s_instance;

    public static MissionManager Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = FindAnyObjectByType<MissionManager>();
                if (s_instance == null)
                {
                    GameObject go = new GameObject("MissionManager");
                    s_instance = go.AddComponent<MissionManager>();
                    Debug.Log("[MissionManager] 씬에 MissionManager가 없어 자동으로 생성하였습니다.");
                }
            }
            return s_instance;
        }
    }

    [Header("미션 전용 오브젝트 프리팹")]
    [SerializeField, FormerlySerializedAs("captureZonePrefab")] private GameObject _captureZonePrefab;      // 거점 점령용 프리팹
    [SerializeField, FormerlySerializedAs("defenseAltarPrefab")] private GameObject _defenseAltarPrefab;     // 제단 방어용 프리팹
    [SerializeField, FormerlySerializedAs("soulHarvestZonePrefab")] private GameObject _soulHarvestZonePrefab;  // 영혼 수집 구역용 프리팹

    [Header("스폰 위치 설정")]
    [SerializeField, FormerlySerializedAs("missionObjectSpawnPosition")] private Vector3 _missionObjectSpawnPosition = Vector3.zero;

    private MissionBase _currentMission;
    private GameObject _spawnedMissionObject;

    public event Action<MissionBase> OnMissionInitialized;
    public event Action<MissionBase> OnMissionCompleted;

    private void Awake()
    {
        if (s_instance == null)
        {
            s_instance = this;
        }
        else if (s_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ★ StageManager.Start()가 단일 진입점으로 미션을 시작하므로 이중 추첨을 방지합니다. ★
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

        if (_currentMission != null)
        {
            _currentMission.Initialize();
            _currentMission.OnMissionCompleted -= HandleMissionCompleted;
            _currentMission.OnMissionCompleted += HandleMissionCompleted;
            OnMissionInitialized?.Invoke(_currentMission);
        }
    }

    private void SetupMissionStrategy(MissionType type)
    {
        MissionBase[] oldMissions = GetComponents<MissionBase>();
        foreach (var old in oldMissions)
        {
            Destroy(old);
        }

        switch (type)
        {
            case MissionType.EnemyKill:
                _currentMission = gameObject.AddComponent<EnemyKillMission>();
                break;

            case MissionType.SoulCollect:
                SoulHarvestZone spawnedHarvestZone = null;
                if (_soulHarvestZonePrefab != null)
                {
                    _spawnedMissionObject = Instantiate(_soulHarvestZonePrefab, _missionObjectSpawnPosition, Quaternion.identity);
                    spawnedHarvestZone = _spawnedMissionObject.GetComponent<SoulHarvestZone>();
                }
                else
                {
                    Debug.LogWarning("[MissionManager] soulHarvestZonePrefab이 연결되지 않았습니다!");
                }

                var soulMission = gameObject.AddComponent<SoulCollectMission>();
                if (spawnedHarvestZone != null)
                {
                    soulMission.SetHarvestZone(spawnedHarvestZone); // ★ 생성된 영혼 수집 구역 직접 주입 ★
                }
                _currentMission = soulMission;
                break;

            case MissionType.AltarDefense:
                DefenseAltar spawnedAltar = null;
                if (_defenseAltarPrefab != null)
                {
                    _spawnedMissionObject = Instantiate(_defenseAltarPrefab, _missionObjectSpawnPosition, Quaternion.identity);
                    spawnedAltar = _spawnedMissionObject.GetComponent<DefenseAltar>();
                }
                else
                {
                    Debug.LogWarning("[MissionManager] defenseAltarPrefab이 연결되지 않았습니다!");
                }

                var altarMission = gameObject.AddComponent<AltarDefenseMission>();
                if (spawnedAltar != null)
                {
                    altarMission.SetTargetAltar(spawnedAltar); // ★ 생성된 제단 직접 주입 ★
                }
                _currentMission = altarMission;
                break;

            case MissionType.Survival:
                _currentMission = gameObject.AddComponent<SurvivalMission>();
                break;

            case MissionType.CaptureZone:
                CaptureZone spawnedCaptureZone = null;
                if (_captureZonePrefab != null)
                {
                    _spawnedMissionObject = Instantiate(_captureZonePrefab, _missionObjectSpawnPosition, Quaternion.identity);
                    spawnedCaptureZone = _spawnedMissionObject.GetComponent<CaptureZone>();
                }
                else
                {
                    Debug.LogWarning("[MissionManager] captureZonePrefab이 연결되지 않았습니다!");
                }

                var captureMission = gameObject.AddComponent<CaptureZoneMission>();
                if (spawnedCaptureZone != null)
                {
                    captureMission.SetTargetZone(spawnedCaptureZone); // ★ 생성된 거점 점령 구역 직접 주입 ★
                }
                _currentMission = captureMission;
                break;
        }
    }

    private void Update()
    {
        if (_currentMission != null && !_currentMission.IsCompleted && !_currentMission.IsFailed)
        {
            _currentMission.UpdateMission(Time.deltaTime);
        }
    }

    private void HandleMissionCompleted()
    {
        Debug.Log("[MissionManager] ★ 미션 달성! StageManager에 포탈 생성 요청");
        OnMissionCompleted?.Invoke(_currentMission);
        if (StageManager.Instance != null)
        {
            StageManager.Instance.HandleMissionCleared();
        }
    }

    public void CleanupMissionObject()
    {
        if (_spawnedMissionObject != null)
        {
            Destroy(_spawnedMissionObject);
            _spawnedMissionObject = null;
            Debug.Log("[MissionManager] 필드 미션 오브젝트 제거 완료");
        }
    }

    public void CleanupCurrentMission()
    {
        if (_currentMission != null)
        {
            _currentMission.OnMissionCompleted -= HandleMissionCompleted;
            Destroy(_currentMission);
            _currentMission = null;
        }

        CleanupMissionObject();
    }

    public MissionBase GetCurrentMission() => _currentMission;
}
