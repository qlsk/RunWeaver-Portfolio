using UnityEngine;

public class StageManager : MonoBehaviour
{
    private static StageManager instance;

    public static StageManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<StageManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("StageManager");
                    instance = go.AddComponent<StageManager>();
                    Debug.Log("[StageManager] 씬에 StageManager가 없어 자동으로 생성하였습니다.");
                }
            }
            return instance;
        }
    }

    [Header("스테이지 상태 정보")]
    [SerializeField] private int currentDepth = 1;

    [Header("포탈 프리팹 및 스폰 위치")]
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private Vector3 portalSpawnPosition = Vector3.zero;

    private GameObject activePortalInstance;

    public int CurrentDepth => currentDepth;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartStage(currentDepth);
    }

    public void ProceedToNextStage()
    {
        currentDepth++;
        Debug.Log($"[StageManager] ===== Depth {currentDepth} 진입 =====");

        DespawnPortal();
        ClearActiveEnemies();
        ResetPlayerPosition();
        UpdateMonsterSpawner();
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

        StopMonsterSpawner();
        ClearActiveEnemies();

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.CleanupMissionObject();
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
            spawner.RestartSpawner(currentDepth);
        }
    }

    public void ReturnToLobby()
    {
        Debug.Log("[StageManager] ===== 로비(Stage 1)로 리셋 =====");
        currentDepth = 1;
        DespawnPortal();
        ClearActiveEnemies();
        ResetPlayerPosition();
        UpdateMonsterSpawner();
        StartStage(currentDepth);
    }
}