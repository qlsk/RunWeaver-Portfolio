using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum MissionType
{
    EnemyKill, // 1. 적 처치
    SoulCollect, // 2. 영혼 수집 (구역 필요)
    AltarDefense, // 3. 제단 방어 (제단 필요)
    Survival, // 4. 제한시간 생존
    CaptureZone // 5. 거점 점령 (거점 필요)
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

    [Header("영혼 수집 구역 배치")]
    [Tooltip("한 번에 스폰하는 구역 수")]
    [SerializeField, Min(1)] private int _soulZoneCount = 3;
    [Tooltip("중심에서 떨어진 거리. 3개가 삼각형 꼭짓점에 놓인다")]
    [SerializeField, Min(0f)] private float _soulZoneTriangleRadius = 6f;

    [Header("미션 추첨")]
    [Tooltip("켜면 항상 Forced Mission Type 미션만 나온다. (테스트용)")]
    [SerializeField] private bool _useForcedMissionType;
    [SerializeField] private MissionType _forcedMissionType = MissionType.SoulCollect;

    [Header("심도별 처치 목표량 (인스펙터 튜닝용)")]
    [Tooltip("적 처치: 1층 기본값 + (심도-1) * 심도당 증가분")]
    [SerializeField, Min(0)] private int _killBaseCount = 5;
    [SerializeField, Min(0)] private int _killPerDepth = 2;
    [Tooltip("엘리트 1킬이 일반 처치 몇 킬로 집계되는지")]
    [SerializeField, Min(1)] private int _eliteKillValue = 3;
    [Tooltip("영혼 수집: 1층 기본값 + (심도-1) * 심도당 증가분")]
    [SerializeField, Min(1)] private int _soulBaseCount = 5;
    [SerializeField, Min(0)] private int _soulPerDepth = 1;

    private MissionBase _currentMission;
    private GameObject _spawnedMissionObject;
    private MissionType? _lastMissionType;
    private MissionType? _nextMissionOverride;
    private readonly List<MissionType> _missionBag = new List<MissionType>();
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

    /// <summary>이번 층의 미션 종류. 준비된 미션이 없으면 null (저장용)</summary>
    public MissionType? CurrentMissionType => _currentMission != null ? _lastMissionType : null;

    /// <summary>다음 층부터 꺼낼 미션 셔플 백 (마지막 원소부터 꺼낸다. 저장용)</summary>
    public IReadOnlyList<MissionType> GetMissionBag() => _missionBag.ToArray();

    /// <summary>이어하기 : 저장된 셔플 백을 되돌린다.</summary>
    public void RestoreMissionBag(IEnumerable<MissionType> bag)
    {
        _missionBag.Clear();
        if (bag != null)
        {
            _missionBag.AddRange(bag);
        }
    }

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

        SetupMissionStrategy(randomType, depth);

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
    /// 미션 종류를 셔플 백으로 뽑습니다. 모든 미션을 섞어 하나씩 꺼내고, 다 꺼내면 다시 섞습니다.
    /// 미션 종류 수만큼의 층마다 모든 미션이 한 번씩 나오며, 직전 층과 같은 미션은 연속으로 나오지 않습니다.
    /// </summary>
    private MissionType PickMissionType()
    {
        if (_nextMissionOverride.HasValue)
        {
            MissionType overridden = _nextMissionOverride.Value;
            _nextMissionOverride = null;
            return overridden;
        }

        if (_useForcedMissionType)
        {
            return _forcedMissionType;
        }

        if (_missionBag.Count == 0)
        {
            RefillMissionBag();
        }

        int lastIndex = _missionBag.Count - 1;
        MissionType picked = _missionBag[lastIndex];
        _missionBag.RemoveAt(lastIndex);
        return picked;
    }

    private void RefillMissionBag()
    {
        _missionBag.Clear();
        _missionBag.AddRange((MissionType[])Enum.GetValues(typeof(MissionType)));

        for (int i = _missionBag.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_missionBag[i], _missionBag[j]) = (_missionBag[j], _missionBag[i]);
        }

        // 백의 마지막 원소부터 꺼내므로, 첫 번째로 나올 미션이 직전 미션과 같으면 맨 앞과 바꿉니다.
        int firstIndex = _missionBag.Count - 1;
        if (_lastMissionType.HasValue && firstIndex > 0 && _missionBag[firstIndex] == _lastMissionType.Value)
        {
            (_missionBag[0], _missionBag[firstIndex]) = (_missionBag[firstIndex], _missionBag[0]);
        }
    }

    /// <summary>
    /// 영혼 수집 구역을 중심에서 떨어진 삼각형 꼭짓점에 스폰한다.
    /// 컨테이너 하나로 묶어 CleanupMissionObject에서 한 번에 정리한다.
    /// </summary>
    private List<SoulHarvestZone> SpawnSoulHarvestZones()
    {
        List<SoulHarvestZone> zones = new();
        if (_soulHarvestZonePrefab == null)
        {
            return zones;
        }

        int count = Mathf.Max(1, _soulZoneCount);
        GameObject container = new("SoulHarvestZones");
        container.transform.position = _missionObjectSpawnPosition;
        _spawnedMissionObject = container;

        // 컨테이너에 표시자를 달아 포탈 위치 계산에서 자식 범위 그림을 제외시킨다.
        // (StageManager가 SoulHarvestZone의 자식은 범위 표시로 보고 bounds에서 뺀다)
        // 이 표시자는 콜라이더가 없어 구역 판정에 쓰이지 않는다.
        container.AddComponent<SoulHarvestZone>();
        Debug.Log("[MissionManager] SoulHarvestZones 컨테이너 생성 + 표시자 부착", this);

        for (int i = 0; i < count; i++)
        {
            float angle = (90f + i * (360f / count)) * Mathf.Deg2Rad;
            Vector3 offset = new(Mathf.Cos(angle) * _soulZoneTriangleRadius, Mathf.Sin(angle) * _soulZoneTriangleRadius, 0f);
            GameObject go = Instantiate(_soulHarvestZonePrefab, _missionObjectSpawnPosition + offset, Quaternion.identity, container.transform);
            SoulHarvestZone zone = go.GetComponent<SoulHarvestZone>();
            if (zone != null)
            {
                zones.Add(zone);
            }
        }

        Debug.Log($"[MissionManager] 영혼 수집 구역 {zones.Count}개 삼각 배치 완료");
        return zones;
    }

    /// <summary>
    /// 심도에 따른 처치 목표량. 1층 기본값 + (심도-1) * 증가분.
    /// </summary>
    private int GetKillTarget(int depth) => _killBaseCount + Mathf.Max(0, depth - 1) * _killPerDepth;

    private int GetSoulTarget(int depth) => _soulBaseCount + Mathf.Max(0, depth - 1) * _soulPerDepth;

    private void SetupMissionStrategy(MissionType type, int depth)
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
                ((EnemyKillMission)_currentMission).SetTargetCounts(
                    GetKillTarget(depth), _eliteKillValue);
                break;

            case MissionType.SoulCollect:
                List<SoulHarvestZone> spawnedHarvestZones = SpawnSoulHarvestZones();

                var soulMission = gameObject.AddComponent<SoulCollectMission>();
                soulMission.SetTargetSoulCount(GetSoulTarget(depth));
                if (spawnedHarvestZones.Count > 0)
                {
                    soulMission.SetHarvestZones(spawnedHarvestZones); // ★ 생성된 영혼 수집 구역들 직접 주입 ★
                }
                else
                {
                    Debug.LogWarning("[MissionManager] soulHarvestZonePrefab이 연결되지 않았습니다!");
                }

                _currentMission = soulMission;
                break;

            case MissionType.AltarDefense:
                DefenseAltar spawnedAltar = null;
                if (_defenseAltarPrefab != null)
                {
                    _spawnedMissionObject = Instantiate(_defenseAltarPrefab, _missionObjectSpawnPosition,
                        Quaternion.identity);
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
                    _spawnedMissionObject =
                        Instantiate(_captureZonePrefab, _missionObjectSpawnPosition, Quaternion.identity);
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

    /// <summary>다음 미션 추첨 한 번을 지정한 종류로 고정한다. (튜토리얼) 추첨에 쓰이면 저절로 해제된다.</summary>
    public void SetNextMissionOverride(MissionType type)
    {
        _nextMissionOverride = type;
    }

    /// <summary>아직 쓰이지 않은 미션 고정을 해제한다.</summary>
    public void ClearNextMissionOverride()
    {
        _nextMissionOverride = null;
    }
}