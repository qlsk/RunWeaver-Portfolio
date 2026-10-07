using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 스테이지 진행(심도, 미션 시작, 포탈 이동)을 관리한다.
///
/// - 전투 씬마다 새로 만들어진다. (DontDestroyOnLoad를 쓰지 않으므로 마을을 다녀오면 심도 1부터 다시 시작한다)
/// - 미션이 클리어되면 스폰을 멈추고 남은 적과 미션 오브젝트를 정리한다. 포탈 등장은 PortalSpawner가 맡는다.
/// - 포탈을 사용하면 귀환(마을 씬) 또는 n층 하강을 처리한다.
/// - 하강하면서 5층 단위를 넘어가면(5→6, 3→6 등) 상점을 연 뒤, 상점을 나오면 다음 층을 시작한다.
/// </summary>
public class StageManager : MonoBehaviour
{
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

    [Header("포탈 프리팹 및 스폰 위치")]
    [SerializeField, FormerlySerializedAs("portalPrefab")] private GameObject _portalPrefab;
    [Tooltip("포탈 3개가 가로로 놓이는 중심 위치")]
    [SerializeField, FormerlySerializedAs("portalSpawnPosition")] private Vector3 _portalSpawnPosition = Vector3.zero;

    [Header("상점")]
    [Tooltip("이 층수 단위를 넘어갈 때 상점을 연다 (5면 5→6, 10→11 ...)")]
    [SerializeField, FormerlySerializedAs("shopFloorInterval"), Min(1)] private int _shopFloorInterval = 5;
    [Tooltip("비워두면 씬에서 찾는다 (꺼진 오브젝트 포함)")]
    [SerializeField, FormerlySerializedAs("shopPresenter")] private ShopPresenter _shopPresenter;
    [Tooltip("비워두면 씬에서 찾는다")]
    [SerializeField, FormerlySerializedAs("popupController")] private PopupController _popupController;

    private PortalSpawner _portalSpawner;
    private bool _isTransitioning;
    private bool _isWaitingShop;

    public int CurrentDepth => _currentDepth;

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
        StartStage(_currentDepth);
    }

    private void OnDestroy()
    {
        if (_shopPresenter != null)
        {
            _shopPresenter.Left -= HandleShopLeft;
        }

        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    // ───────────────────────── 미션 클리어 ─────────────────────────

    /// <summary>
    /// 미션 클리어 시 MissionManager가 호출한다. 스폰을 멈추고 남은 적과 미션 오브젝트를 정리한다.
    /// </summary>
    public void HandleMissionCleared()
    {
        StopMonsterSpawner();
        ClearActiveEnemies();

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.CleanupMissionObject();
        }
    }

    // ───────────────────────── 포탈 ─────────────────────────

    /// <summary>
    /// 포탈에 F로 상호작용했을 때 Portal이 호출한다.
    /// </summary>
    public void UsePortal(Portal portal)
    {
        if (portal == null || _isTransitioning)
        {
            return;
        }

        if (portal.Type == PortalType.Return)
        {
            ReturnToTown();
        }
        else
        {
            Descend(portal.Floors);
        }
    }

    private void Descend(int floors)
    {
        _isTransitioning = true;

        int from = _currentDepth;
        int to = _currentDepth + Mathf.Max(1, floors);
        _currentDepth = to;
        Debug.Log($"[StageManager] ===== Depth {from} → {to} =====");

        DespawnPortal();

        if (ShouldOpenShop(from, to) && TryOpenShop())
        {
            return; // 상점을 나오면(HandleShopLeft) 다음 층을 시작한다.
        }

        StartNextStage();
    }

    private void ReturnToTown()
    {
        _isTransitioning = true;
        Debug.Log("[StageManager] ===== 마을로 귀환 =====");

        // TODO: 귀환 정산 (획득 자원 확보, 결과 화면) 연결
        DespawnPortal();
        SceneLoader.LoadTownScene();
    }

    /// <summary>
    /// from 층에서 to 층으로 갈 때 상점 단위(5층)를 넘어가는지.
    /// 4→5, 3→5는 넘어가지 않고 5→6, 3→6, 9→12는 넘어간다.
    /// </summary>
    public bool ShouldOpenShop(int from, int to)
    {
        if (to <= from)
        {
            return false;
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
        UpdateMonsterSpawner();
        StartStage(_currentDepth);
        _isTransitioning = false;
    }

    public void DespawnPortal()
    {
        if (_portalSpawner != null)
        {
            _portalSpawner.DespawnPortals();
        }
    }

    // ───────────────────────── 기존 호환 ─────────────────────────

    /// <summary>
    /// [기존 테스트 UI 호환] 1층 하강과 같다.
    /// </summary>
    public void ProceedToNextStage()
    {
        if (_isTransitioning)
        {
            return;
        }

        Descend(1);
    }

    /// <summary>
    /// [기존 테스트 UI 호환] 씬 이동 없이 심도 1로 되돌린다. 실제 귀환은 귀환 포탈(ReturnToTown)을 사용한다.
    /// </summary>
    public void ReturnToLobby()
    {
        Debug.Log("[StageManager] ===== 로비(Stage 1)로 리셋 =====");
        _currentDepth = 1;
        DespawnPortal();
        StartNextStage();
    }

    // ───────────────────────── 내부 처리 ─────────────────────────

    private void StartStage(int depth)
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.StartRandomMission(depth);
        }
        else
        {
            Debug.LogWarning("[StageManager] MissionManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    private void StopMonsterSpawner()
    {
        MonsterSpawner spawner = MonsterSpawner.Instance != null ? MonsterSpawner.Instance : FindAnyObjectByType<MonsterSpawner>();
        if (spawner != null)
        {
            spawner.StopSpawning();
        }
    }

    private void ClearActiveEnemies()
    {
        EnemyHealth[] realEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in realEnemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
    }

    private void ResetPlayerPosition()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.transform.position = Vector3.zero;
        }
    }

    private void UpdateMonsterSpawner()
    {
        MonsterSpawner spawner = MonsterSpawner.Instance != null ? MonsterSpawner.Instance : FindAnyObjectByType<MonsterSpawner>();
        if (spawner != null)
        {
            spawner.RestartSpawner(_currentDepth);
        }
    }
}
