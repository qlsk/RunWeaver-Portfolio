using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    private static MonsterSpawner instance;

    public static MonsterSpawner Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<MonsterSpawner>();
            }
            return instance;
        }
    }

    [Header("몬스터 프리팹 데이터베이스 (프리팹 직접 등록)")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();

    [Header("예산(Budget) 스케일링 설정")]
    [SerializeField] private int baseBudget = 20;
    [SerializeField] private int budgetPerDepth = 10;

    [Header("최대 마릿수(Count) 스케일링 설정")]
    [SerializeField] private int baseMaxMonsterCount = 15;
    [SerializeField] private int monsterCountPerDepth = 5;

    [Header("스폰 주기 및 티어 가속 계수")]
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private float tierGrowthMultiplier = 5f;

    [Header("스폰 거리 및 연출")]
    [SerializeField] private float minSpawnDistance = 8f;
    [SerializeField] private float maxSpawnDistance = 12f;
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float effectDuration = 1f;

    private Transform playerTransform;
    private Coroutine spawnCoroutine;

    private class SpawnableEntry
    {
        public GameObject Prefab;
        public EnemyDataSO Data;

        public int MinDepth => Data != null && Data.Info != null ? Data.Info.MinDepth : 1;
        public int Cost => Data != null && Data.Info != null ? Data.Info.Cost : 1;

        public float GetCalculatedWeight(int depth, float multiplier)
        {
            if (Data != null && Data.Info != null)
            {
                return Data.Info.GetCalculatedWeight(depth, multiplier);
            }
            return 100f;
        }
    }

    private readonly List<SpawnableEntry> spawnableEntries = new List<SpawnableEntry>();
    private readonly Dictionary<EnemyHealth, int> activeMonsterCostMap = new Dictionary<EnemyHealth, int>();
    private int currentActiveBudget = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EnemyHealth.OnDeath += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleEnemyDied;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        CacheEnemyPrefabs();

        // 스테이지 흐름(StageManager)이 있는 씬에서는 시작 오브젝트 상호작용으로 미션이 시작될 때
        // StageManager가 RestartSpawner로 스폰을 시작한다. StageManager가 없는 테스트 씬에서만 바로 시작한다.
        StageManager stageManager = FindAnyObjectByType<StageManager>();
        if (stageManager == null)
        {
            StartSpawning(1);
        }
        else if (stageManager.Phase == StagePhase.InMission && spawnCoroutine == null)
        {
            StartSpawning(stageManager.CurrentDepth);
        }
    }

    private void CacheEnemyPrefabs()
    {
        spawnableEntries.Clear();

        enemyPrefabs = enemyPrefabs.Where(p => p != null).ToList();

        if (enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("[MonsterSpawner] enemyPrefabs에 유효한 프리팹이 없습니다! Project 창의 02_Prefabs/Enemy/DefaultEnemy 프리팹을 인스펙터에 끌어다 놓아주세요.");
            return;
        }

        int validCount = 0;
        foreach (var prefab in enemyPrefabs)
        {
            EnemyDataSO data = null;
            if (prefab.TryGetComponent(out EnemyHealth health) || (health = prefab.GetComponentInChildren<EnemyHealth>()) != null)
            {
                data = health.EnemyData;
            }

            spawnableEntries.Add(new SpawnableEntry
            {
                Prefab = prefab,
                Data = data
            });
            validCount++;
        }

        Debug.Log($"[MonsterSpawner] 몬스터 프리팹 {validCount}종 캐싱 완료.");
    }

    public void StartSpawning(int depth)
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(ContinuousSpawnRoutine(depth));
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        foreach (var pair in activeMonsterCostMap.ToList())
        {
            if (pair.Key != null && pair.Key.gameObject != null)
            {
                Destroy(pair.Key.gameObject);
            }
        }
        activeMonsterCostMap.Clear();
        currentActiveBudget = 0;

        Debug.Log("[MonsterSpawner] 몬스터 스폰 코루틴 중지 및 활성 몬스터 정리 완료");
    }

    /// <summary>
    /// 스폰만 멈춘다. 이미 나온 몬스터는 그대로 둔다. (미션 클리어 후 정화 대기, 런 종료)
    /// </summary>
    public void PauseSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    /// <summary>
    /// 등록된 몬스터 프리팹 중 첫 번째를 돌려준다. (보스 프리팹이 없을 때 임시 보스로 사용)
    /// </summary>
    public GameObject GetFallbackEnemyPrefab()
    {
        foreach (GameObject prefab in enemyPrefabs)
        {
            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    public void RestartSpawner(int newDepth)
    {
        StopSpawning();
        CacheEnemyPrefabs();
        StartSpawning(newDepth);
        Debug.Log($"[MonsterSpawner] Depth {newDepth}로 스포너 재시작");
    }

    public int GetMaxActiveBudget(int depth) => baseBudget + (depth - 1) * budgetPerDepth;
    public int GetMaxMonsterCount(int depth) => baseMaxMonsterCount + (depth - 1) * monsterCountPerDepth;

    private IEnumerator ContinuousSpawnRoutine(int depth)
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            CleanStaleMonsters();

            if (spawnableEntries.Count == 0)
            {
                CacheEnemyPrefabs();
                if (spawnableEntries.Count == 0) continue;
            }

            int maxBudget = GetMaxActiveBudget(depth);
            int maxCount = GetMaxMonsterCount(depth);
            int availableBudget = maxBudget - currentActiveBudget;

            while (availableBudget > 0 && activeMonsterCostMap.Count < maxCount)
            {
                // ★ 현재 Depth 조건(MinDepth <= depth) 및 남은 예산(Cost <= availableBudget)을 충족하는 풀만 추출 ★
                var eligiblePool = spawnableEntries
                    .Where(e => e.Prefab != null && e.MinDepth <= depth && e.Cost <= availableBudget)
                    .ToList();

                // 조건에 맞는 몬스터가 없으면 강제 대체 풀을 사용하지 않고 이번 스폰 시도를 중단(대기)
                if (eligiblePool.Count == 0) break;

                SpawnableEntry selectedEntry = SelectByTierWeight(eligiblePool, depth);
                if (selectedEntry == null) break;

                if (SpawnMonster(selectedEntry))
                {
                    currentActiveBudget += selectedEntry.Cost;
                    availableBudget -= selectedEntry.Cost;
                }
                else
                {
                    break;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private SpawnableEntry SelectByTierWeight(List<SpawnableEntry> pool, int depth)
    {
        List<float> weights = pool
            .Select(e => e.GetCalculatedWeight(depth, tierGrowthMultiplier))
            .ToList();

        float totalWeight = weights.Sum();
        if (totalWeight <= 0f) return pool[UnityEngine.Random.Range(0, pool.Count)];

        float randomVal = UnityEngine.Random.Range(0f, totalWeight);
        float currentSum = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            currentSum += weights[i];
            if (randomVal <= currentSum) return pool[i];
        }

        return pool[0];
    }

    private bool SpawnMonster(SpawnableEntry entry)
    {
        if (entry == null || entry.Prefab == null) return false;

        if (playerTransform == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) playerTransform = pObj.transform;
            else return false;
        }

        Vector2 spawnPos = GetRandomSpawnPosition();

        GameObject spawned = Instantiate(entry.Prefab, spawnPos, Quaternion.identity);

        EnemyHealth health = spawned.GetComponent<EnemyHealth>() ?? spawned.GetComponentInChildren<EnemyHealth>();
        if (health != null)
        {
            if (spawnEffectPrefab != null)
            {
                GameObject fx = Instantiate(spawnEffectPrefab, spawnPos, Quaternion.identity);
                Destroy(fx, effectDuration);
            }

            activeMonsterCostMap[health] = entry.Cost;
            return true;
        }
        else
        {
            Debug.LogWarning($"[MonsterSpawner] '{entry.Prefab.name}' 프리팹에 EnemyHealth 컴포넌트가 없어 스폰을 취소합니다.");
            Destroy(spawned);
            return false;
        }
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 center = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float distance = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);

        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (enemy != null && activeMonsterCostMap.TryGetValue(enemy, out int cost))
        {
            currentActiveBudget = Mathf.Max(0, currentActiveBudget - cost);
            activeMonsterCostMap.Remove(enemy);
        }
    }

    private void CleanStaleMonsters()
    {
        var deadKeys = activeMonsterCostMap.Keys.Where(k => k == null || k.gameObject == null).ToList();
        foreach (var key in deadKeys)
        {
            currentActiveBudget = Mathf.Max(0, currentActiveBudget - activeMonsterCostMap[key]);
            activeMonsterCostMap.Remove(key);
        }
    }
}