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

    [Header("미션 추첨")]
    [Tooltip("직전 층과 같은 미션이 뽑힐 가중치 (다른 미션은 1). 0이면 연속으로 나오지 않는다.")]
    [SerializeField, Range(0f, 1f)] private float _repeatedMissionWeight = 0.25f;

    private MissionBase _currentMission;
    private GameObject _spawnedMissionObject;
    private MissionType? _lastMissionType;
    private bool _isMissionRunning;

    /// <summary>새 미션이 정해졌을 때 (아직 진행 전. HUD 표시용)</summary>
    public event Action<MissionBase> OnMissionInitialized;
    /// <summary>시작 오브젝트 상호작용으로 미션 진행이 시작되었을 때</summary>
    public event Action<MissionBase> OnMissionStarted;
    public event Action<MissionBase> OnMissionCompleted;
    /// <summary>미션이 실패했을 때 (제단 파괴, 생존 미션 중 사망)</summary>
    public event Action<MissionBase> OnMissionFailed;

    /// <summary>현재 미션이 깔아 둔 미션 오브젝트 (거점, 제단, 영혼 수집 구역). 없으면 null</summary>
    public GameObject CurrentMissionObject => _spawnedMissionObject;

    /// <summary>미션 진행 중인지 (시작 오브젝트 상호작용 후 완료/실패 전)</summary>
    public bool IsMissionRunning => _isMissionRunning;

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
    /// 미션 진행은 BeginCurrentMission을 부를 때 시작합니다. (시작 오브젝트 상호작용)
    /// </summary>
    public MissionBase PrepareRandomMission(int depth)
    {
        CleanupCurrentMission();

        MissionType randomType = PickMissionType();
        _lastMissionType = randomType;

        Debug.Log($"[MissionManager] ===== Depth {depth} 무작위 미션 추첨: {randomType} =====");

        SetupMissionStrategy(randomType);

        if (_currentMission != null)
        {
            OnMissionInitialized?.Invoke(_currentMission);
        }

        return _currentMission;
    }

    /// <summary>
    /// 준비된 미션의 진행을 시작합니다. 이때부터 처치 수, 점령 게이지, 생존 시간 등이 집계됩니다.
    /// </summary>
    public void BeginCurrentMission()
    {
        if (_currentMission == null || _isMissionRunning)
        {
            return;
        }

        _currentMission.Initialize();
        _currentMission.OnMissionCompleted -= HandleMissionCompleted;
        _currentMission.OnMissionCompleted += HandleMissionCompleted;
        _currentMission.OnMissionFailed -= HandleMissionFailed;
        _currentMission.OnMissionFailed += HandleMissionFailed;
        _isMissionRunning = true;

        Debug.Log($"[MissionManager] 미션 시작: {_currentMission.GetType().Name}");
        OnMissionStarted?.Invoke(_currentMission);
    }

    /// <summary>
    /// [기존 호환] 미션을 추첨하고 바로 시작합니다.
    /// </summary>
    public void StartRandomMission(int depth)
    {
        PrepareRandomMission(depth);
        BeginCurrentMission();
    }

    /// <summary>
    /// 미션 종류를 가중치로 뽑습니다. 직전 층과 같은 미션은 가중치가 낮습니다.
    /// </summary>
    private MissionType PickMissionType()
    {
        MissionType[] types = (MissionType[])Enum.GetValues(typeof(MissionType));
        float[] weights = new float[types.Length];
        float totalWeight = 0f;

        for (int i = 0; i < types.Length; i++)
        {
            weights[i] = _lastMissionType.HasValue && types[i] == _lastMissionType.Value ? _repeatedMissionWeight : 1f;
            totalWeight += weights[i];
        }

        if (totalWeight <= 0f)
        {
            return types[UnityEngine.Random.Range(0, types.Length)];
        }

        float pick = UnityEngine.Random.Range(0f, totalWeight);
        for (int i = 0; i < types.Length; i++)
        {
            pick -= weights[i];
            if (pick < 0f)
            {
                return types[i];
            }
        }

        return types[types.Length - 1];
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
        if (_isMissionRunning && _currentMission != null && !_currentMission.IsCompleted && !_currentMission.IsFailed)
        {
            _currentMission.UpdateMission(Time.deltaTime);
        }
    }

    private void HandleMissionCompleted()
    {
        _isMissionRunning = false;
        Debug.Log("[MissionManager] ★ 미션 달성! StageManager에 정화와 포탈 생성 요청");
        OnMissionCompleted?.Invoke(_currentMission);
        if (StageManager.Instance != null)
        {
            StageManager.Instance.HandleMissionCleared();
        }
    }

    private void HandleMissionFailed()
    {
        _isMissionRunning = false;
        Debug.Log("[MissionManager] ✖ 미션 실패! StageManager에 런 종료 요청");
        OnMissionFailed?.Invoke(_currentMission);
    }

    /// <summary>
    /// 미션 진행을 멈춘다. (런 종료) 미션과 미션 오브젝트는 그대로 둔다.
    /// </summary>
    public void StopCurrentMission()
    {
        _isMissionRunning = false;
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
        _isMissionRunning = false;

        if (_currentMission != null)
        {
            _currentMission.OnMissionCompleted -= HandleMissionCompleted;
            _currentMission.OnMissionFailed -= HandleMissionFailed;
            Destroy(_currentMission);
            _currentMission = null;
        }

        CleanupMissionObject();
    }

    public MissionBase GetCurrentMission() => _currentMission;
}