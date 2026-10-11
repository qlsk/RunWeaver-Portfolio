using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 스테이지 진행(심도, 미션 시작, 정화, 포탈 이동, 런 종료)을 관리한다.
/// 전투 씬마다 생성되며, 미션 층을 모두 끝내면 보스전으로 넘어간다.
/// </summary>
public class StageManager : MonoBehaviour
{
    private const float NoTimeLimit = -1f;

    private static StageManager s_instance;

    public static StageManager Instance
    {
        get
        {
            if (s_instance != null)
                return s_instance;

            s_instance = FindAnyObjectByType<StageManager>();

            if (s_instance != null)
                return s_instance;

            var go = new GameObject("StageManager");
            return !go.TryGetComponent(out s_instance)
                ? throw new InvalidOperationException("go did not have a StageManager component.")
                : s_instance;
        }
    }

    [Header("References")]
    [SerializeField] private PlayerInteraction _playerInteraction;

    [Header("스테이지 상태 정보")]
    [SerializeField, FormerlySerializedAs("currentDepth")] private int _currentDepth = 1;

    [Header("런 구성")]
    [Tooltip("미션 이름과 설명. 비워두면 Resources/MissionCatalog를 사용한다.")]
    [SerializeField] private MissionCatalogSO _missionCatalog;
    [Tooltip("미션 스테이지 수. 이 수만큼 미션을 끝내면 다음 층이 보스 스테이지다.")]
    [SerializeField, Min(1)] private int _missionStageCount = 10;
    [SerializeField, Min(0f)] private float _autoStartDelay = 10f;
    [Tooltip("미션 제한 시간(초). 생존 미션은 미션의 생존 시간을 제한 시간으로 쓴다.")]
    [SerializeField, Min(1f)] private float _missionTimeLimit = 120f;

    [Header("시작 오브젝트")]
    [Tooltip("미션 오브젝트가 없는 미션(적 처치, 생존)과 보스 스테이지의 시작 오브젝트 위치")]
    [SerializeField] private Vector3 _startObjectPosition = Vector3.zero;

    [Header("출발 준비")]
    [Tooltip("켜면 씬을 열어도 1층을 바로 시작하지 않고, 시작 석상(ExpeditionStatue)이 BeginPreparedRun을 부를 때까지 기다린다.")]
    [SerializeField] private bool _waitForPreparation = true;

    [Header("정화")]
    [Tooltip("미션 클리어 후 남은 적이 사라지기까지의 정화 대기 시간(초)")]
    [SerializeField, Min(0f)] private float _purificationDuration = 10f;
    [Tooltip("미션 클리어 시 퍼지는 정화 파동. 미션 오브젝트(붙어 있는 파동) 위치, 없으면 시작 오브젝트 위치에 이 프리팹을 만든다. 비워두면 정화 카운트다운을 쓴다.")]
    [SerializeField] private PurificationWave _purificationWavePrefab;

    [Header("포탈")]
    [SerializeField, FormerlySerializedAs("portalPrefab")] private GameObject _portalPrefab;
    [Tooltip("PortalSpawner에 전달할 기본 포탈 위치")]
    [SerializeField, FormerlySerializedAs("portalSpawnPosition")] private Vector3 _portalSpawnPosition = Vector3.zero;
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

    [Header("보스 스테이지")]
    [Tooltip("비워두면 MonsterSpawner에 등록된 첫 번째 적을 보스 대신 쓴다")]
    [SerializeField] private GameObject _bossPrefab;
    [SerializeField] private Vector3 _bossSpawnPosition = new(0f, 6f, 0f);

    // 스테이지 진행
    private StagePhase _phase = StagePhase.Ready;
    private float _timeLimit;
    private float _remainingTime;
    private bool _isTransitioning;

    // 미션과 시작 오브젝트
    private MissionManager _missionManager;
    private MissionBase _currentMission;
    private IMissionRewardProvider _missionRewardProvider;

    // 정화 연출
    private Vector3 _waveOrigin;
    private PurificationWave _activeWave;
    private readonly System.Collections.Generic.List<SpriteRenderer> _rangeRenderers = new();

    // 포탈 등장
    private PortalSpawner _portalSpawner;
    private Coroutine _portalRoutine;
    private bool _isPortalOpen;
    private Vector3 _portalPosition;

    // 상점 대기
    private bool _isWaitingShop;

    // 보스전과 플레이어 사망
    private bool _isBossStage;
    private bool _isBossDefeated;
    private EnemyHealth _bossHealth;
    private PlayerHealth _playerHealth;

    private MissionStartInteractable _startInteractable;

    private GameObject CurrentMissionObject => _missionManager != null ? _missionManager.CurrentMissionObject : null;
    /// <summary>포탈을 쓸 수 있는 단계인지 (포탈이 들어가기 연출 전에 확인한다)</summary>
    public bool CanUsePortal => _phase is StagePhase.Purification or StagePhase.Cleared;

    public int CurrentDepth => _currentDepth;
    public int MissionStageCount => _missionStageCount;
    public int BossDepth => _missionStageCount + 1;
    public StagePhase Phase => _phase;
    public bool IsBossStage => _isBossStage;

    /// <summary>이번 층의 보스. 보스 층이 아니거나 아직 나오지 않았으면 null (보스 처치 연출이 처치된 보스를 찾을 때 쓴다)</summary>
    public EnemyHealth BossHealth => _bossHealth;
    public MissionBase CurrentMission => _currentMission;

    /// <summary>다음 층으로 가는 포탈이 나와 있는지</summary>
    public bool IsPortalOpen => _isPortalOpen;

    public float AutostartRemaining { get; private set; }
    public bool IsAutoStartCounting => _autoStartDelay > 0f && _startInteractable != null && _phase == StagePhase.Ready;

    /// <summary>
    /// 타이머에 보여줄 남은 시간(초).
    /// Ready : 미션 제한 시간 전체 / InMission : 미션 남은 시간 / Purification : 정화 남은 시간.
    /// 제한 시간이 없으면(보스 스테이지) NoTimeLimit.
    /// </summary>
    public float RemainingTime => _remainingTime;

    private MissionCatalogSO.MissionText CurrentMissionText
    {
        get
        {
            if (_missionCatalog == null)
            {
                return null;
            }

            if (_isBossStage)
            {
                return _missionCatalog.BossMission;
            }

            return _currentMission != null ? _missionCatalog.Get(_currentMission.Type) : null;
        }
    }

    /// <summary>현재 목표 이름 (HUD 표시용)</summary>
    public string ObjectiveName => CurrentMissionText?.DisplayName ?? string.Empty;

    /// <summary>현재 목표 설명 (HUD 표시용). 제단 방어 중에는 제단 HP를 실시간으로 덧붙인다.</summary>
    public string ObjectiveDescription
    {
        get
        {
            string baseText = CurrentMissionText?.Description ?? string.Empty;
            if (_currentMission is AltarDefenseMission altar && altar.TargetAltar != null)
            {
                DefenseAltar target = altar.TargetAltar;
                string hpText = $"제단 HP: {target.CurrentHp:F0}/{target.MaxHp:F0}";
                return string.IsNullOrEmpty(baseText) ? hpText : $"{baseText} ({hpText})";
            }
            return baseText;
        }
    }

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

    // ───────────────────────── 생명주기 ─────────────────────────

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

        if (_missionCatalog == null)
        {
            _missionCatalog = Resources.Load<MissionCatalogSO>("MissionCatalog");
        }

        _portalSpawner = GetComponent<PortalSpawner>();
        if (_portalSpawner == null)
        {
            _portalSpawner = gameObject.AddComponent<PortalSpawner>();
        }

        _portalSpawner.Configure(_portalPrefab, _portalSpawnPosition);

        if (_playerInteraction == null)
            _playerInteraction = FindAnyObjectByType<PlayerInteraction>();
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

        // 시작 석상에서 원소와 룬을 고르기 전에는 1층 미션을 준비하지 않는다.
        if (_waitForPreparation)
        {
            IsWaitingForPreparation = true;
            return;
        }

        StartStage(_currentDepth);
    }

    /// <summary>출발 준비(시작 석상)를 기다리는 중인지</summary>
    public bool IsWaitingForPreparation { get; private set; }

    /// <summary>
    /// 출발 준비가 끝났을 때 시작 석상이 부른다. 1층 미션을 준비하고 시작 오브젝트를 만든다. (StageStarted 발생)
    /// 기다리는 중이 아니면 아무것도 하지 않는다.
    /// </summary>
    public void BeginPreparedRun()
    {
        if (!IsWaitingForPreparation)
        {
            return;
        }

        IsWaitingForPreparation = false;
        StartStage(_currentDepth);
    }

    private void Update()
    {
        switch (_phase)
        {
            case StagePhase.Ready:
                UpdateAutoStart();
                break;
            case StagePhase.InMission:
                UpdateMissionTimer();
                break;
            case StagePhase.Purification:
                UpdatePurification();
                break;
        }
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

    // ───────────────────────── 층 준비와 시작 ─────────────────────────

    private void StartStage(int depth)
    {
        _isTransitioning = false;
        StopPortalRoutine();

        // 정화 중에 포탈로 내려왔으면 이전 층의 파동은 더 기다리지 않는다.
        DetachWave();
        _isBossStage = depth > _missionStageCount;
        _isBossDefeated = false;
        _bossHealth = null;
        _currentMission = null;
        UnregisterStartInteractable();

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
                GameObject missionObject = CurrentMissionObject;
                if (missionObject != null)
                {
                    startPosition = missionObject.transform.position;
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
        RegisterStartInteractable();
        AutostartRemaining = _autoStartDelay;
        SetPhase(StagePhase.Ready);
        SafeEventNotifier.Notify(StageStarted, depth);
    }

    private void RegisterStartInteractable()
    {
        _startInteractable = new MissionStartInteractable(this, "임무 시작");

        if (_playerInteraction != null)
            _playerInteraction.SetFallbackInteractable(_startInteractable);
    }

    private void UnregisterStartInteractable()
    {
        if (_startInteractable == null)
            return;

        if (_playerInteraction != null)
            _playerInteraction.ClearFallbackInteractable(_startInteractable);

        _startInteractable = null;
    }

    /// <summary>시작 오브젝트에 F로 상호작용하면 미션(보스 스테이지는 보스전)을 시작한다.</summary>
    public void HandleStartActivated()
    {
        if (_phase != StagePhase.Ready)
        {
            return;
        }

        UnregisterStartInteractable();
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

    // ───────────────────────── 미션 진행과 보상 ─────────────────────────

    private void UpdateAutoStart()
    {
        if (_autoStartDelay <= 0f)
            return;

        if (_startInteractable == null)
            return;

        AutostartRemaining -= Time.deltaTime;

        if (AutostartRemaining <= 0f)
            HandleStartActivated();
    }

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
    /// 미션 클리어 보상을 등록한다. (예: 업그레이드 3택1) 등록하지 않으면 클리어 후 바로 포탈이 나온다.
    /// </summary>
    public void SetMissionRewardProvider(IMissionRewardProvider provider)
    {
        _missionRewardProvider = provider;
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

    // ───────────────────────── 정화와 범위 표시 ─────────────────────────

    /// <summary>
    /// 미션 오브젝트 또는 시작 위치에 독립된 정화 파동을 만든다.
    /// 붙어 있는 파동은 원본을 끄고, 프리팹이 없을 때 복제해 사용한다.
    /// </summary>
    private PurificationWave PrepareWave()
    {
        PurificationWave wave = null;
        Vector3 origin = _waveOrigin;

        GameObject missionObject = CurrentMissionObject;
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
        DetachWave();
        CompletePurification();
    }

    /// <summary>정화 파동의 완료 이벤트 구독과 참조를 해제한다.</summary>
    private void DetachWave()
    {
        if (_activeWave != null)
        {
            _activeWave.Finished -= HandleWaveFinished;
            _activeWave = null;
        }
    }

    private void CompletePurification()
    {
        if (_phase != StagePhase.Purification)
        {
            return;
        }

        // Destroy는 사망 이벤트를 보내지 않으므로 드롭 없이 정리한다.
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

        CompletePurification();
    }

    /// <summary>영혼 수집 구역의 자식 또는 거점 점령 구역 본체의 범위 표시를 모은다. 정화 파동은 제외한다.</summary>
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
        GameObject missionObject = CurrentMissionObject;
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

    // ───────────────────────── 포탈 등장과 사용 ─────────────────────────

    /// <summary>미션 성공 연출이 끝날 때까지 기다렸다가 포탈을 만든다.</summary>
    private void SpawnPortalAfterPresentation()
    {
        StopPortalRoutine();
        _portalRoutine = StartCoroutine(SpawnPortalAfterPresentationRoutine());
    }

    private void StopPortalRoutine()
    {
        if (_portalRoutine != null)
        {
            StopCoroutine(_portalRoutine);
            _portalRoutine = null;
        }
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

    /// <summary>
    /// 포탈 위치 : 미션 오브젝트 그림(범위 표시와 정화 파동 제외)의 아래쪽.
    /// 그림이 범위 표시뿐이면 오브젝트 중앙, 미션 오브젝트가 없으면 시작 오브젝트가 있던 자리 아래.
    /// </summary>
    private Vector3 GetPortalPosition()
    {
        GameObject missionObject = CurrentMissionObject;

        // 영혼 미션: 삼각형 중심(컨테이너 위치)에 고정. bounds 계산을 타지 않는다.
        if (missionObject != null && missionObject.name == "SoulHarvestZones")
        {
            Vector3 center = missionObject.transform.position;
            return new Vector3(center.x, center.y, 0f);
        }

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
        if (!CanUsePortal)
        {
            return;
        }

        if (_portalSpawner != null)
        {
            string label = _currentDepth + 1 >= BossDepth ? "Face the Boss" : "Descend";
            _portalSpawner.SpawnPortal(label, _portalPosition);
            _isPortalOpen = true;
        }
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
    /// 포탈에 F로 상호작용했을 때 Portal이 호출한다. 한 층 내려간다.
    /// </summary>
    public void UsePortal(Portal portal)
    {
        if (portal == null || _isTransitioning)
        {
            return;
        }

        if (!CanUsePortal)
        {
            return;
        }

        Descend();
    }

    // ───────────────────────── 층 이동과 상점 ─────────────────────────

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

    private void StartNextStage()
    {
        ClearActiveEnemies();
        ResetPlayerPosition();
        StartStage(_currentDepth);
    }

    // ───────────────────────── 보스전 ─────────────────────────

    private void SpawnBoss()
    {
        GameObject prefab = _bossPrefab;
        if (prefab == null)
        {
            MonsterSpawner spawner = MonsterSpawner.Instance;
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

    // ───────────────────────── 런 종료 ─────────────────────────

    private void HandlePlayerDied()
    {
        EndRun(RunEndReason.Died);
    }

    private void HandleMissionFailed(MissionBase mission)
    {
        // 생존 미션은 플레이어가 죽어도 미션 실패를 보내므로 사망을 우선한다.
        bool isPlayerDead = _playerHealth != null && _playerHealth.IsDead;
        EndRun(isPlayerDead ? RunEndReason.Died : RunEndReason.MissionFailed);
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
        UnregisterStartInteractable();
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

    private void RestartMonsterSpawner()
    {
        MonsterSpawner spawner = MonsterSpawner.Instance;
        if (spawner != null)
        {
            spawner.RestartSpawner(_currentDepth);
        }
    }

    /// <summary>스폰만 멈춘다. 이미 나온 적은 그대로 둔다.</summary>
    private void PauseMonsterSpawner()
    {
        MonsterSpawner spawner = MonsterSpawner.Instance;
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

    /// <summary>카메라 추적 Rig를 플레이어 위치로 바로 옮긴다.</summary>
    private static void SnapCameraToPlayer(Vector3 playerPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        CameraFollow cameraFollow = mainCamera.GetComponentInParent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.SnapToPosition(playerPosition);
            return;
        }

        Vector3 cameraPosition = mainCamera.transform.position;
        cameraPosition.x = playerPosition.x;
        cameraPosition.y = playerPosition.y;
        mainCamera.transform.position = cameraPosition;
    }
}
