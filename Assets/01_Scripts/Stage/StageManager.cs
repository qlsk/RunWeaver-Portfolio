using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 스테이지 진행(심도, 미션 시작, 정화, 포탈 이동, 런 종료)을 관리한다.
///
/// 한 층의 흐름 (StagePhase)
/// - Ready : 미션을 뽑고 시작 오브젝트를 놓는다. 적이 나오지 않고 타이머가 멈춰 있다.
/// - InMission : 시작 오브젝트에 F로 상호작용하면 미션과 몬스터 스폰이 시작되고 미션 타이머가 줄어든다.
///   제한 시간이 0이 되면 미션 실패(런 종료). 생존 미션은 생존 시간이 끝나면 완료된다.
/// - Purification : 미션을 클리어하면 스폰과 미션 타이머가 멈추고 정화 파동(PurificationWave)이 퍼진다.
///   파동은 미션 오브젝트 중심(거점·제단·영혼 수집) 또는 시작 오브젝트 위치(적 처치·생존)에서 시작하고,
///   닿은 적은 사망 연출과 함께 사라진다. (드롭 없음) 파동이 끝까지 퍼지면 Cleared로 넘어간다.
///   파동 프리팹이 없으면 예전처럼 정화 카운트다운(_purificationDuration)이 끝날 때 남은 적을 없앤다.
///   포탈은 미션 성공 연출(MissionResultPresenter)이 끝난 뒤에 나온다. (연출이 없으면 바로)
///   미션 오브젝트는 삭제하지 않고 오브젝트에 붙어 있는 범위 표시만 숨긴다. 포탈은 미션 오브젝트 아래에 나온다.
///   (범위 표시 : 영혼 수집 구역의 자식 원(Circle), 거점 점령 구역의 마법진. 제단은 범위 표시가 없다)
///   (범위 표시만 있는 거점 점령은 오브젝트 중앙, 오브젝트가 없는 미션은 시작 오브젝트가 있던 자리 아래)
///   포탈은 클리어 즉시 맵 중앙에 생기며 정화 중에도 쓸 수 있다. 남은 적은 계속 공격하고 처치하면 평소처럼 드롭한다.
/// - Cleared : 정화가 끝나면 남은 적이 모두 사라진다. (드롭 없음) 포탈 사용을 기다린다.
/// - 포탈을 쓰면 한 층 내려간다. 상점 층 단위(5층)를 넘거나 보스 스테이지로 갈 때는 상점을 먼저 연다.
///   상점이 없으면 ScreenFader로 검은 화면 → 다음 층 준비 → 페이드 아웃 순서로 넘어간다. (상점은 나갈 때 자체 검은 화면을 쓴다)
///
/// 런 구성
/// - 미션 스테이지 _missionStageCount(10)개를 끝내면 다음 층은 보스 스테이지다.
///   보스 스테이지는 시작 오브젝트에 F를 누르면 보스가 나오고, 보스를 잡으면 런 클리어다.
///
/// 런 종료 (RunEnded)
/// - 체력 0 : Died / 미션 제한 시간 초과·제단 파괴 : MissionFailed / 탐사 포기 : Abandoned / 보스 처치 : Cleared
/// - 실패로 끝나면 플레이어를 사망 처리해 조작을 멈춘다. 종료 사유는 RunFlow에 기록된다.
///
/// - 전투 씬마다 새로 만들어진다. (DontDestroyOnLoad를 쓰지 않으므로 마을을 다녀오면 심도 1부터 다시 시작한다)
/// </summary>
public class StageManager : MonoBehaviour
{
    public const float NoTimeLimit = -1f;

    private const float StartObjectRadius = 1.5f;

    private static StageManager s_instance;

    public static StageManager Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = FindAnyObjectByType<StageManager>();
                if (s_instance == null)
                {
                    GameObject go = new GameObject("StageManager");
                    s_instance = go.AddComponent<StageManager>();
                    Debug.Log("[StageManager] 씬에 StageManager가 없어 자동으로 생성하였습니다.");
                }
            }
            return s_instance;
        }
    }

    [Header("스테이지 상태 정보")]
    [SerializeField, FormerlySerializedAs("currentDepth")] private int _currentDepth = 1;

    [Header("런 구성")]
    [Tooltip("미션 스테이지 수. 이 수만큼 미션을 끝내면 다음 층이 보스 스테이지다.")][SerializeField, Min(1)] private int _missionStageCount = 10;
    [Tooltip("미션 제한 시간(초). 생존 미션은 미션의 생존 시간을 제한 시간으로 쓴다.")][SerializeField, Min(1f)]
    private float _missionTimeLimit = 120f;
    [Tooltip("미션 클리어 후 남은 적이 사라지기까지의 정화 대기 시간(초)")][SerializeField, Min(0f)] private float _purificationDuration = 10f;

    [Header("시작 오브젝트")]
    [Tooltip("비워두면 코드로 임시 시작 오브젝트(원형 + F - Start)를 만든다")][SerializeField] private MissionStartObject _startObjectPrefab;
    [Tooltip("미션 오브젝트가 없는 미션(적 처치, 생존)과 보스 스테이지의 시작 오브젝트 위치")][SerializeField]
    private Vector3 _startObjectPosition = new(0f, 2f, 0f);
    [Tooltip("임시 시작 오브젝트를 놓을 레이어. PlayerInteraction의 Interactable Layer와 같아야 한다.")][SerializeField]
    private int _startObjectLayer = 6;

    [Header("보스 스테이지")]
    [Tooltip("비워두면 MonsterSpawner에 등록된 첫 번째 적을 보스 대신 쓴다")][SerializeField] private GameObject _bossPrefab;
    [SerializeField] private Vector3 _bossSpawnPosition = new(0f, 6f, 0f);

    [Header("포탈 프리팹 및 스폰 위치")]
    [SerializeField, FormerlySerializedAs("portalPrefab")] private GameObject _portalPrefab;
    [Tooltip("포탈이 놓이는 위치 (맵 중앙)")]
    [SerializeField, FormerlySerializedAs("portalSpawnPosition")] private Vector3 _portalSpawnPosition = Vector3.zero;

    [Header("정화 파동")]
    [Tooltip("미션 클리어 시 퍼지는 정화 파동. 미션 오브젝트(붙어 있는 파동) 위치, 없으면 시작 오브젝트 위치에 이 프리팹을 만든다. 비워두면 정화 카운트다운을 쓴다.")]
    [SerializeField] private PurificationWave _purificationWavePrefab;

    [Header("포탈 등장")]
    [Tooltip("미션 오브젝트(또는 시작 오브젝트 자리) 아래로 포탈을 띄우는 거리(유닛)")]
    [SerializeField, Min(0f)] private float _portalOffsetBelow = 1.5f;
    [Tooltip("이 연출이 끝난 뒤에 포탈을 만든다. 비워두면 씬에서 찾고, 없으면 바로 만든다.")]
    [SerializeField] private MissionResultPresenter _missionResultPresenter;

    [Header("상점")]
    [Tooltip("이 층수 단위를 넘어갈 때 상점을 연다 (5면 5→6 ...). 보스 스테이지로 갈 때도 연다.")]
    [SerializeField, FormerlySerializedAs("shopFloorInterval"), Min(1)] private int _shopFloorInterval = 5;
    [Tooltip("비워두면 씬에서 찾는다 (꺼진 오브젝트 포함)")]
    [SerializeField, FormerlySerializedAs("shopPresenter")] private ShopPresenter _shopPresenter;
    [Tooltip("비워두면 씬에서 찾는다")]
    [SerializeField, FormerlySerializedAs("popupController")] private PopupController _popupController;

    private PortalSpawner _portalSpawner;
    private MissionManager _missionManager;
    private PlayerHealth _playerHealth;
    private MissionBase _currentMission;
    private MissionStartObject _startObject;
    private EnemyHealth _bossHealth;
    private IMissionRewardProvider _missionRewardProvider;
    private StagePhase _phase = StagePhase.Ready;
    private float _timeLimit;
    private float _remainingTime;
    private bool _isBossStage;
    private bool _isBossDefeated;
    private bool _isTransitioning;
    private bool _isWaitingShop;
    private Vector3 _waveOrigin;
    private PurificationWave _activeWave;
    private Coroutine _portalRoutine;
    private bool _isPortalOpen;
    private Vector3 _portalPosition;
    private readonly System.Collections.Generic.List<SpriteRenderer> _rangeRenderers = new();

    public int CurrentDepth => _currentDepth;
    public int MissionStageCount => _missionStageCount;
    public int BossDepth => _missionStageCount + 1;
    public StagePhase Phase => _phase;
    public bool IsBossStage => _isBossStage;
    public MissionBase CurrentMission => _currentMission;

    /// <summary>다음 층으로 가는 포탈이 나와 있는지</summary>
    public bool IsPortalOpen => _isPortalOpen;

    /// <summary>
    /// 타이머에 보여줄 남은 시간(초).
    /// Ready : 미션 제한 시간 전체 / InMission : 미션 남은 시간 / Purification : 정화 남은 시간.
    /// 제한 시간이 없으면(보스 스테이지) NoTimeLimit.
    /// </summary>
    public float RemainingTime => _remainingTime;

    /// <summary>현재 목표 이름 (HUD 표시용)</summary>
    public string ObjectiveName => _isBossStage ? "보스 처치" : GetMissionName(_currentMission);

    /// <summary>현재 목표 달성도 (0 ~ 1)</summary>
    public float ObjectiveProgress
    {
        get
        {
            if (_isBossStage)
            {
                return _isBossDefeated ? 1f : 0f;
            }

            if (_currentMission == null)
            {
                return 0f;
            }

            return _currentMission.IsCompleted ? 1f : Mathf.Clamp01(_currentMission.GetProgressRatio());
        }
    }

    /// <summary>층에 들어와 미션이 정해졌을 때 (심도)</summary>
    public event Action<int> StageStarted;

    /// <summary>진행 단계가 바뀌었을 때</summary>
    public event Action<StagePhase> PhaseChanged;

    /// <summary>미션을 클리어했을 때 (심도). 보스 처치는 RunEnded(Cleared)로 알린다.</summary>
    public event Action<int> MissionCleared;

    /// <summary>런이 끝났을 때 (종료 사유)</summary>
    public event Action<RunEndReason> RunEnded;

    private void Awake()
    {
        if (s_instance == null)
        {
            s_instance = this;
        }
        else if (s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _portalSpawner = GetComponent<PortalSpawner>();
        if (_portalSpawner == null)
        {
            _portalSpawner = gameObject.AddComponent<PortalSpawner>();
        }

        _portalSpawner.Configure(_portalPrefab, _portalSpawnPosition);
    }

    private void Start()
    {
        RunFlow.BeginRun();

        _missionManager = MissionManager.Instance;
        if (_missionManager != null)
        {
            _missionManager.OnMissionFailed += HandleMissionFailed;
        }

        _playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (_playerHealth != null)
        {
            _playerHealth.Died += HandlePlayerDied;
        }
        else
        {
            Debug.LogWarning("[StageManager] PlayerHealth를 찾지 못해 사망으로 런을 끝낼 수 없습니다.", this);
        }

        EnemyHealth.OnDeath += HandleEnemyDied;

        // 시작 오브젝트를 누르기 전에는 적이 없어야 하므로 씬에 미리 놓인 적을 정리한다.
        ClearActiveEnemies();
        StartStage(_currentDepth);
    }

    private void OnDestroy()
    {
        EnemyHealth.OnDeath -= HandleEnemyDied;

        if (_missionManager != null)
        {
            _missionManager.OnMissionFailed -= HandleMissionFailed;
        }

        if (_playerHealth != null)
        {
            _playerHealth.Died -= HandlePlayerDied;
        }

        if (_shopPresenter != null)
        {
            _shopPresenter.Left -= HandleShopLeft;
        }

        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    private void Update()
    {
        switch (_phase)
        {
            case StagePhase.InMission:
                UpdateMissionTimer();
                break;
            case StagePhase.Purification:
                UpdatePurification();
                break;
        }
    }

    /// <summary>
    /// 미션 클리어 보상을 등록한다. (예: 업그레이드 3택1) 등록하지 않으면 클리어 후 바로 포탈이 나온다.
    /// </summary>
    public void SetMissionRewardProvider(IMissionRewardProvider provider)
    {
        _missionRewardProvider = provider;
    }

    // ───────────────────────── 층 시작 ─────────────────────────

    private void StartStage(int depth)
    {
        _isTransitioning = false;

        if (_portalRoutine != null)
        {
            StopCoroutine(_portalRoutine);
            _portalRoutine = null;
        }

        // 정화 중에 포탈로 내려왔으면 이전 층의 파동은 더 기다리지 않는다.
        if (_activeWave != null)
        {
            _activeWave.Finished -= HandleWaveFinished;
            _activeWave = null;
        }
        _isBossStage = depth > _missionStageCount;
        _isBossDefeated = false;
        _bossHealth = null;
        _currentMission = null;
        DestroyStartObject();

        Vector3 startPosition = _startObjectPosition;
        bool isOnMissionObject = false;

        if (_isBossStage)
        {
            if (_missionManager != null)
            {
                _missionManager.CleanupCurrentMission();
            }

            _timeLimit = NoTimeLimit;
            Debug.Log($"[StageManager] ===== Depth {depth} 보스 스테이지 =====");
        }
        else
        {
            if (_missionManager != null)
            {
                _currentMission = _missionManager.PrepareRandomMission(depth);
                GameObject missionObject = _missionManager.CurrentMissionObject;
                if (missionObject != null)
                {
                    startPosition = missionObject.transform.position;
                    isOnMissionObject = true;
                }
            }
            else
            {
                Debug.LogWarning("[StageManager] MissionManager 인스턴스를 찾을 수 없습니다.", this);
            }

            _timeLimit = _currentMission is SurvivalMission survival ? survival.SurvivalDuration : _missionTimeLimit;
        }

        _remainingTime = _timeLimit;
        _waveOrigin = startPosition;
        SpawnStartObject(startPosition, !isOnMissionObject);
        SetPhase(StagePhase.Ready);
        SafeEventNotifier.Notify(StageStarted, depth);
    }

    private void SpawnStartObject(Vector3 position, bool showMarker)
    {
        _startObject = _startObjectPrefab != null
            ? Instantiate(_startObjectPrefab, position, Quaternion.identity)
            : MissionStartObject.Create(position, _startObjectLayer, StartObjectRadius);

        _startObject.SetMarkerVisible(showMarker);
        _startObject.Activated += HandleStartActivated;
    }

    private void DestroyStartObject()
    {
        if (_startObject == null)
        {
            return;
        }

        _startObject.Activated -= HandleStartActivated;
        Destroy(_startObject.gameObject);
        _startObject = null;
    }

    /// <summary>시작 오브젝트에 F로 상호작용하면 미션(보스 스테이지는 보스전)을 시작한다.</summary>
    private void HandleStartActivated()
    {
        if (_phase != StagePhase.Ready)
        {
            return;
        }

        DestroyStartObject();
        SetPhase(StagePhase.InMission);

        if (_isBossStage)
        {
            SpawnBoss();
            return;
        }

        if (_missionManager != null)
        {
            _missionManager.BeginCurrentMission();
        }

        RestartMonsterSpawner();
    }

    // ───────────────────────── 미션 진행 ─────────────────────────

    private void UpdateMissionTimer()
    {
        if (_timeLimit < 0f)
        {
            return;
        }

        _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);

        // 생존 미션은 생존 시간이 끝나면 미션이 스스로 완료된다.
        if (_remainingTime <= 0f && _currentMission is not SurvivalMission)
        {
            Debug.Log("[StageManager] 미션 제한 시간 초과 → 미션 실패");
            EndRun(RunEndReason.MissionFailed);
        }
    }

    /// <summary>
    /// 미션 클리어 시 MissionManager가 호출한다. 스폰을 멈추고 정화 파동(없으면 정화 카운트다운)을 시작한 뒤 포탈을 만든다.
    /// </summary>
    public void HandleMissionCleared()
    {
        if (_phase != StagePhase.InMission)
        {
            return;
        }

        // 포탈 위치는 미션 오브젝트의 범위 표시를 숨기기 전에 정한다.
        _portalPosition = GetPortalPosition();
        _activeWave = PrepareWave();
        _remainingTime = _activeWave != null ? 0f : _purificationDuration;
        SetPhase(StagePhase.Purification);
        PauseMonsterSpawner();

        if (_activeWave != null)
        {
            _activeWave.Finished += HandleWaveFinished;
            if (!_activeWave.IsPlaying)
            {
                _activeWave.PlayDetached();
            }
        }

        // 미션 오브젝트는 남겨 두고 범위 표시만 숨긴다. (다음 층을 시작할 때 MissionManager가 정리한다)
        HideMissionRange();

        SafeEventNotifier.Notify(MissionCleared, _currentDepth);

        if (_missionRewardProvider != null)
        {
            _missionRewardProvider.Grant(_currentDepth, SpawnPortalAfterPresentation);
        }
        else
        {
            SpawnPortalAfterPresentation();
        }
    }

    /// <summary>미션 성공 연출이 끝날 때까지 기다렸다가 포탈을 만든다.</summary>
    private void SpawnPortalAfterPresentation()
    {
        if (_portalRoutine != null)
        {
            StopCoroutine(_portalRoutine);
        }

        _portalRoutine = StartCoroutine(SpawnPortalAfterPresentationRoutine());
    }

    private System.Collections.IEnumerator SpawnPortalAfterPresentationRoutine()
    {
        if (_missionResultPresenter == null)
        {
            _missionResultPresenter = FindAnyObjectByType<MissionResultPresenter>();
        }

        // 성공 연출은 미션 완료 이벤트로 시작되므로 한 프레임 기다려 시작 여부를 확인한다.
        yield return null;

        while (_missionResultPresenter != null && _missionResultPresenter.IsShowingSuccess)
        {
            yield return null;
        }

        _portalRoutine = null;
        SpawnNextPortal();
    }

    private void HandleMissionFailed(MissionBase mission)
    {
        // 생존 미션은 플레이어가 죽어도 미션 실패를 보내므로 사망을 우선한다.
        bool isPlayerDead = _playerHealth != null && _playerHealth.IsDead;
        EndRun(isPlayerDead ? RunEndReason.Died : RunEndReason.MissionFailed);
    }

    /// <summary>
    /// 정화 파동을 새로 만든다. 위치는 미션 오브젝트에 붙은 파동(없으면 미션 오브젝트, 시작 오브젝트) 위치다.
    /// 미션 오브젝트에 붙은 파동은 위치만 쓰고 꺼 둔다. (제단 방어처럼 클리어 순간 미션 오브젝트를 먼저 삭제하는
    /// 미션에서는 붙은 파동이 오브젝트와 함께 사라지므로, 항상 따로 만든 파동을 쓴다) 닿은 적을 정화하도록 켠다.
    /// </summary>
    private PurificationWave PrepareWave()
    {
        PurificationWave wave = null;
        Vector3 origin = _waveOrigin;

        GameObject missionObject = _missionManager != null ? _missionManager.CurrentMissionObject : null;
        PurificationWave attachedWave = missionObject != null
            ? missionObject.GetComponentInChildren<PurificationWave>(true)
            : null;

        if (attachedWave != null)
        {
            origin = attachedWave.transform.position;
        }
        else if (missionObject != null)
        {
            origin = missionObject.transform.position;
        }

        origin.z = 0f;

        if (_purificationWavePrefab != null)
        {
            wave = Instantiate(_purificationWavePrefab, origin, Quaternion.identity);
        }
        else if (attachedWave != null)
        {
            // 프리팹이 없으면 붙은 파동을 복제해서 쓴다. (복제한 뒤 원본을 끈다)
            wave = Instantiate(attachedWave, origin, Quaternion.identity);
            wave.transform.localScale = Vector3.one;
        }

        if (attachedWave != null)
        {
            attachedWave.enabled = false;
        }

        if (wave != null)
        {
            wave.PurifyEnemies = true;
        }
        else
        {
            Debug.LogWarning("[StageManager] 정화 파동이 없어 정화 카운트다운으로 정화합니다. " +
                             "(Purification Wave Prefab 연결과 미션 오브젝트의 파동을 확인하세요)", this);
        }

        return wave;
    }

    /// <summary>파동이 끝까지 퍼지면 화면 밖에 남은 적을 지우고 정화를 끝낸다.</summary>
    private void HandleWaveFinished()
    {
        if (_activeWave != null)
        {
            _activeWave.Finished -= HandleWaveFinished;
            _activeWave = null;
        }

        if (_phase != StagePhase.Purification)
        {
            return;
        }

        ClearActiveEnemies();
        SetPhase(StagePhase.Cleared);
    }

    private void UpdatePurification()
    {
        // 정화 파동이 퍼지는 중이면 파동이 끝날 때(HandleWaveFinished) 넘어간다.
        if (_activeWave != null)
        {
            return;
        }

        _remainingTime = Mathf.Max(0f, _remainingTime - Time.deltaTime);
        if (_remainingTime > 0f)
        {
            return;
        }

        // 정화 파동 : 남은 적을 드롭 없이 없앤다. (Destroy는 사망 이벤트를 보내지 않는다)
        ClearActiveEnemies();
        SetPhase(StagePhase.Cleared);
    }

    // ───────────────────────── 보스 ─────────────────────────

    private void SpawnBoss()
    {
        GameObject prefab = _bossPrefab;
        if (prefab == null)
        {
            MonsterSpawner spawner = FindSpawner();
            prefab = spawner != null ? spawner.GetFallbackEnemyPrefab() : null;
        }

        if (prefab == null)
        {
            Debug.LogError("[StageManager] 보스로 쓸 프리팹이 없어 보스 스테이지를 클리어 처리합니다.", this);
            DefeatBoss();
            return;
        }

        GameObject boss = Instantiate(prefab, _bossSpawnPosition, Quaternion.identity);
        _bossHealth = boss.GetComponent<EnemyHealth>();
        if (_bossHealth == null)
        {
            _bossHealth = boss.GetComponentInChildren<EnemyHealth>();
        }

        if (_bossHealth == null)
        {
            Debug.LogError($"[StageManager] 보스 프리팹 '{prefab.name}'에 EnemyHealth가 없어 클리어 처리합니다.", this);
            DefeatBoss();
            return;
        }

        Debug.Log($"[StageManager] 보스 등장: {prefab.name}");
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (_isBossStage && _phase == StagePhase.InMission && enemy != null && enemy == _bossHealth)
        {
            DefeatBoss();
        }
    }

    private void DefeatBoss()
    {
        _isBossDefeated = true;
        Debug.Log("[StageManager] ★ 보스 처치! 런 클리어 ★");
        EndRun(RunEndReason.Cleared);
    }

    // ───────────────────────── 포탈 ─────────────────────────

    /// <summary>
    /// 미션 오브젝트에 이미 붙어 있는 범위 표시 렌더러를 모은다.
    /// - 영혼 수집 구역 : 본체(영혼 그림)를 뺀 자식 렌더러 (범위 원 Circle)
    /// - 거점 점령 구역 : 본체의 마법진 (구역 그림 자체가 범위다)
    /// 정화 파동의 렌더러는 넣지 않는다.
    /// </summary>
    private void CollectRangeRenderers(GameObject missionObject)
    {
        _rangeRenderers.Clear();
        if (missionObject == null)
        {
            return;
        }

        if (missionObject.TryGetComponent(out SoulHarvestZone _))
        {
            foreach (SpriteRenderer renderer in missionObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.gameObject != missionObject && renderer.GetComponentInParent<PurificationWave>() == null)
                {
                    _rangeRenderers.Add(renderer);
                }
            }
        }
        else if (missionObject.TryGetComponent(out CaptureZone _) &&
                 missionObject.TryGetComponent(out SpriteRenderer zoneRenderer))
        {
            _rangeRenderers.Add(zoneRenderer);
        }
    }

    /// <summary>미션 오브젝트의 범위 표시를 서서히 숨긴다. (오브젝트 자체는 남긴다)</summary>
    private void HideMissionRange()
    {
        GameObject missionObject = _missionManager != null ? _missionManager.CurrentMissionObject : null;
        CollectRangeRenderers(missionObject);
        if (_rangeRenderers.Count > 0)
        {
            StartCoroutine(FadeOutRangeRoutine(new System.Collections.Generic.List<SpriteRenderer>(_rangeRenderers)));
        }
    }

    private System.Collections.IEnumerator FadeOutRangeRoutine(System.Collections.Generic.List<SpriteRenderer> renderers)
    {
        const float FadeDuration = 0.4f;
        var baseAlphas = new float[renderers.Count];
        for (int i = 0; i < renderers.Count; i++)
        {
            baseAlphas[i] = renderers[i] != null ? renderers[i].color.a : 0f;
        }

        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / FadeDuration);
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                Color color = renderers[i].color;
                color.a = baseAlphas[i] * t;
                renderers[i].color = color;
            }

            yield return null;
        }

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// 포탈 위치 : 미션 오브젝트 그림(범위 표시와 정화 파동 제외)의 아래쪽.
    /// 그림이 범위 표시뿐이면 오브젝트 중앙, 미션 오브젝트가 없으면 시작 오브젝트가 있던 자리 아래.
    /// </summary>
    private Vector3 GetPortalPosition()
    {
        GameObject missionObject = _missionManager != null ? _missionManager.CurrentMissionObject : null;
        if (missionObject == null)
        {
            return new Vector3(_waveOrigin.x, _waveOrigin.y - _portalOffsetBelow, 0f);
        }

        CollectRangeRenderers(missionObject);
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (SpriteRenderer renderer in missionObject.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled || _rangeRenderers.Contains(renderer) ||
                renderer.GetComponentInParent<PurificationWave>() != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            Vector3 center = missionObject.transform.position;
            return new Vector3(center.x, center.y, 0f);
        }

        return new Vector3(bounds.center.x, bounds.min.y - _portalOffsetBelow, 0f);
    }

    private void SpawnNextPortal()
    {
        // 보상 선택 중에 런이 끝났으면 포탈을 만들지 않는다.
        if (_phase != StagePhase.Purification && _phase != StagePhase.Cleared)
        {
            return;
        }

        if (_portalSpawner != null)
        {
            string label = _currentDepth + 1 >= BossDepth ? "보스에게로" : "하강";
            _portalSpawner.SpawnPortal(label, _portalPosition);
            _isPortalOpen = true;
        }
    }

    /// <summary>
    /// 포탈에 F로 상호작용했을 때 Portal이 호출한다. 한 층 내려간다.
    /// </summary>
    public void UsePortal(Portal portal)
    {
        if (portal == null || _isTransitioning)
        {
            return;
        }

        if (_phase != StagePhase.Purification && _phase != StagePhase.Cleared)
        {
            return;
        }

        Descend();
    }

    private void Descend()
    {
        _isTransitioning = true;

        int from = _currentDepth;
        int to = _currentDepth + 1;
        _currentDepth = to;
        Debug.Log($"[StageManager] ===== Depth {from} → {to} =====");

        DespawnPortal();
        ClearActiveEnemies();

        if (ShouldOpenShop(from, to) && TryOpenShop())
        {
            SetPhase(StagePhase.Shop);
            return; // 상점을 나오면(HandleShopLeft) 다음 층을 시작한다.
        }

        TransitionToNextStage();
    }

    /// <summary>
    /// 화면을 검게 덮은 동안 다음 층을 준비하고 걷어낸다.
    /// 플레이어가 순간이동하고 카메라가 맵을 가로지르는 모습이 보이지 않게 한다.
    /// </summary>
    private void TransitionToNextStage()
    {
        ScreenFader fader = ScreenFader.Instance;
        if (fader == null)
        {
            StartNextStage();
            return;
        }

        fader.FadeThroughBlack(() =>
        {
            // 검은 화면 도중에 런이 끝났으면 다음 층을 시작하지 않는다.
            if (this == null || _phase == StagePhase.Ended)
            {
                return;
            }

            StartNextStage();
        });
    }

    /// <summary>
    /// from 층에서 to 층으로 갈 때 상점을 여는지.
    /// 상점 층 단위(5층)를 넘어가거나(5→6) 보스 스테이지로 들어갈 때(10→보스) 연다.
    /// </summary>
    public bool ShouldOpenShop(int from, int to)
    {
        if (to <= from)
        {
            return false;
        }

        if (to == BossDepth)
        {
            return true;
        }

        return (Mathf.Max(1, from) - 1) / _shopFloorInterval != (to - 1) / _shopFloorInterval;
    }

    private bool TryOpenShop()
    {
        if (_shopPresenter == null)
        {
            _shopPresenter = FindAnyObjectByType<ShopPresenter>(FindObjectsInactive.Include);
        }

        if (_popupController == null)
        {
            _popupController = FindAnyObjectByType<PopupController>(FindObjectsInactive.Include);
        }

        if (_shopPresenter == null)
        {
            Debug.LogWarning("[StageManager] 씬에 ShopPresenter가 없어 상점을 건너뜁니다.", this);
            return false;
        }

        _isWaitingShop = true;
        _shopPresenter.Left -= HandleShopLeft;
        _shopPresenter.Left += HandleShopLeft;

        // 팝업으로 열어서 상점이 열린 동안 게임 입력을 막는다.
        if (_popupController != null)
        {
            _popupController.OpenPopup(_shopPresenter.gameObject);
        }

        _shopPresenter.OpenShop();
        return true;
    }

    private void HandleShopLeft()
    {
        if (!_isWaitingShop)
        {
            return;
        }

        _isWaitingShop = false;
        _shopPresenter.Left -= HandleShopLeft;

        if (_popupController != null)
        {
            _popupController.ClosePopup();
        }

        StartNextStage();
    }

    private void StartNextStage()
    {
        ClearActiveEnemies();
        ResetPlayerPosition();
        StartStage(_currentDepth);
    }

    public void DespawnPortal()
    {
        _isPortalOpen = false;
        if (_portalSpawner != null)
        {
            _portalSpawner.DespawnPortals();
        }
    }

    /// <summary>
    /// [기존 테스트 UI 호환] 지금 층을 끝내고 한 층 내려간다.
    /// </summary>
    public void ProceedToNextStage()
    {
        if (_isTransitioning || _phase == StagePhase.Ended || _phase == StagePhase.Shop)
        {
            return;
        }

        PauseMonsterSpawner();
        Descend();
    }

    // ───────────────────────── 런 종료 ─────────────────────────

    private void HandlePlayerDied()
    {
        EndRun(RunEndReason.Died);
    }

    /// <summary>
    /// 런을 끝낸다. 처음 기록된 종료 사유가 남는다. (탐사 포기는 PauseController가 먼저 기록한다)
    /// 실패로 끝나면 플레이어를 사망 처리해 조작을 멈춘다.
    /// </summary>
    private void EndRun(RunEndReason reason)
    {
        if (_phase == StagePhase.Ended)
        {
            return;
        }

        RunFlow.TrySetEndReason(reason);
        RunEndReason finalReason = RunFlow.EndReason ?? reason;
        Debug.Log($"[StageManager] ===== 런 종료: {finalReason} =====");

        SetPhase(StagePhase.Ended);
        DestroyStartObject();
        PauseMonsterSpawner();
        DespawnPortal();

        // 런이 끝난 뒤에 미션이 계속 진행되어 완료되지 않도록 멈춘다.
        if (_missionManager != null && !_isBossStage)
        {
            _missionManager.StopCurrentMission();
        }

        if (finalReason != RunEndReason.Cleared && _playerHealth != null && !_playerHealth.IsDead)
        {
            _playerHealth.Kill();
        }

        SafeEventNotifier.Notify(RunEnded, finalReason);
    }

    // ───────────────────────── 내부 처리 ─────────────────────────

    private void SetPhase(StagePhase phase)
    {
        if (_phase == phase)
        {
            return;
        }

        _phase = phase;
        SafeEventNotifier.Notify(PhaseChanged, phase);
    }

    private static string GetMissionName(MissionBase mission)
    {
        return mission switch
        {
            EnemyKillMission => "적 처치",
            SoulCollectMission => "구역 안의 적 처치",
            AltarDefenseMission => "제단 방어",
            SurvivalMission => "생존",
            CaptureZoneMission => "거점 점령",
            null => string.Empty,
            _ => "임무"
        };
    }

    private static MonsterSpawner FindSpawner()
    {
        return MonsterSpawner.Instance != null ? MonsterSpawner.Instance : FindAnyObjectByType<MonsterSpawner>();
    }

    private void RestartMonsterSpawner()
    {
        MonsterSpawner spawner = FindSpawner();
        if (spawner != null)
        {
            spawner.RestartSpawner(_currentDepth);
        }
    }

    /// <summary>스폰만 멈춘다. 이미 나온 적은 그대로 둔다.</summary>
    private void PauseMonsterSpawner()
    {
        MonsterSpawner spawner = FindSpawner();
        if (spawner != null)
        {
            spawner.PauseSpawning();
        }
    }

    /// <summary>남은 적과 적 탄환을 모두 지운다. (정화 완료, 다음 층 시작, 런 종료)</summary>
    private void ClearActiveEnemies()
    {
        EnemyHealth[] realEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in realEnemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }

        // 적 탄환은 플레이어에 맞을 때만 사라지므로, 층이 바뀌어도 날아오지 않게 함께 지운다.
        EnemyBullet[] enemyBullets = FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None);
        foreach (EnemyBullet bullet in enemyBullets)
        {
            if (bullet != null) Destroy(bullet.gameObject);
        }
    }

    private void ResetPlayerPosition()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.transform.position = Vector3.zero;
            SnapCameraToPlayer(playerObj.transform.position);
        }
    }

    /// <summary>카메라가 맵을 가로질러 따라가지 않도록 플레이어 위치로 바로 옮긴다. (z는 유지)</summary>
    private static void SnapCameraToPlayer(Vector3 playerPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 cameraPosition = mainCamera.transform.position;
        cameraPosition.x = playerPosition.x;
        cameraPosition.y = playerPosition.y;
        mainCamera.transform.position = cameraPosition;
    }
}