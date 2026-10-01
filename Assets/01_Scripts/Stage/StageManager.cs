using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("스테이지 상태 정보")]
    [SerializeField] private int currentDepth = 1;

    [Header("포탈 프리팹 및 스폰 위치")]
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private Vector3 portalSpawnPosition = Vector3.zero;

    private GameObject activePortalInstance;

    public int CurrentDepth => currentDepth;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartStage(currentDepth);
    }

    /// <summary>
    /// ReturnConfirmPopupUI의 [계속 진행] 버튼 클릭 시 호출됩니다.
    /// 씬 전환 없이 동일 맵에서 Depth를 +1 올리고 다음 미션을 실행합니다.
    /// </summary>
    public void ProceedToNextStage()
    {
        currentDepth++;
        Debug.Log($"[StageManager] ===== [계속 진행] 클릭 -> Depth {currentDepth} 진입 =====");

        // 1. 생성되어 있던 포탈 제거
        DespawnPortal();

        // 2. 맵상의 남아있는 모든 적 제거
        ClearActiveEnemies();

        // 3. 플레이어 위치 초기화
        ResetPlayerPosition();

        // 4. 몬스터 스포너 난이도 및 스폰 주기 데이터 갱신
        UpdateMonsterSpawner();

        // 5. 신규 무작위 미션 시작
        StartStage(currentDepth);
    }

    public void SpawnPortalAtCenter()
    {
        if (activePortalInstance != null) return;

        if (portalPrefab != null)
        {
            activePortalInstance = Instantiate(portalPrefab, portalSpawnPosition, Quaternion.identity);
            Debug.Log("[StageManager] 포탈이 생성되었습니다.");
        }
        else
        {
            Debug.LogWarning("[StageManager] portalPrefab이 인스펙터에 연결되지 않았습니다!");
        }
    }

    public void DespawnPortal()
    {
        if (activePortalInstance != null)
        {
            Destroy(activePortalInstance);
            activePortalInstance = null;
        }
    }

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

    private void ClearActiveEnemies()
    {
        DummyEnemy[] enemies = FindObjectsByType<DummyEnemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        // 실제 몬스터(EnemyHealth)도 함께 제거한다.
        EnemyHealth[] realEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in realEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
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
        MonsterSpawner spawner = FindAnyObjectByType<MonsterSpawner>();
        if (spawner != null)
        {
            spawner.RestartSpawner(currentDepth);
        }
    }

    /// <summary>
    /// ReturnConfirmPopupUI의 [복귀] 버튼 클릭 시 호출됩니다.
    /// </summary>
    public void ReturnToLobby()
    {
        Debug.Log("[StageManager] ===== [복귀] 클릭 -> 로비(Stage 1)로 리셋 =====");
        currentDepth = 1;
        DespawnPortal();
        ClearActiveEnemies();
        ResetPlayerPosition();
        UpdateMonsterSpawner();
        StartStage(currentDepth);
    }
}