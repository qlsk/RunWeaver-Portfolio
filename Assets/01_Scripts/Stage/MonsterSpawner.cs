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

        int startDepth = StageManager.Instance != null ? StageManager.Instance.CurrentDepth : 1;
        StartSpawning(startDepth);
    }

    private void CacheEnemyPrefabs()
    {
        spawnableEntries.Clear();

        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("[MonsterSpawner] enemyPrefabs 리스트가 비어있습니다! 인스펙터의 Enemy Prefabs 항목에 DefaultEnemy 프리팹을 추가해주세요.");
            return;
        }

        int validCount = 0;
        foreach (var prefab in enemyPrefabs)
        {
            if (prefab == null) continue;

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
                var eligiblePool = spawnableEntries
                    .Where(e => e.Prefab != null && e.MinDepth <= depth && e.Cost <= availableBudget)
                    .ToList();

                if (eligiblePool.Count == 0)
                {
                    eligiblePool = spawnableEntries.Where(e => e.Prefab != null).ToList();
                    if (eligiblePool.Count == 0) break;
                }

                SpawnableEntry selectedEntry = SelectByTierWeight(eligiblePool, depth);
                if (selectedEntry == null) break;

                SpawnMonster(selectedEntry);

                currentActiveBudget += selectedEntry.Cost;
                availableBudget -= selectedEntry.Cost;

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

    private void SpawnMonster(SpawnableEntry entry)
    {
        if (entry == null || entry.Prefab == null) return;

        if (playerTransform == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) playerTransform = pObj.transform;
            else return;
        }

        Vector2 spawnPos = GetRandomSpawnPosition();

        if (spawnEffectPrefab != null)
        {
            GameObject fx = Instantiate(spawnEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(fx, effectDuration);
        }

        GameObject spawned = Instantiate(entry.Prefab, spawnPos, Quaternion.identity);

        EnemyHealth health = spawned.GetComponent<EnemyHealth>() ?? spawned.GetComponentInChildren<EnemyHealth>();
        if (health != null)
        {
            activeMonsterCostMap[health] = entry.Cost;
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