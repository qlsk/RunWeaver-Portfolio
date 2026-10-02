using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("몬스터 프리팹 데이터베이스 (프리팹 직접 등록)")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();

    [Header("예산(Budget) 스케일링 설정")]
    [SerializeField] private int baseBudget = 20;             // Depth 1 기본 예산
    [SerializeField] private int budgetPerDepth = 10;         // Depth당 추가 예산 (+10)

    [Header("최대 마릿수(Count) 스케일링 설정")]
    [SerializeField] private int baseMaxMonsterCount = 15;    // Depth 1 기본 최대 마릿수
    [SerializeField] private int monsterCountPerDepth = 5;    // Depth당 추가 마릿수 (+5)

    [Header("스폰 주기 및 티어 가속 계수")]
    [SerializeField] private float spawnInterval = 0.5f;      // 리젠 감시 및 스폰 주기
    [SerializeField] private float tierGrowthMultiplier = 5f; // 티어 가중치 가속 계수 (α)

    [Header("스폰 거리 및 연출")]
    [SerializeField] private float minSpawnDistance = 8f;     // 플레이어 기준 화면 밖 거리
    [SerializeField] private float maxSpawnDistance = 12f;
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float effectDuration = 1f;

    private Transform playerTransform;
    private Coroutine spawnCoroutine;

    // 프리팹 내부 데이터를 런타임에 캐싱하기 위한 내부 클래스
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
            return 100f; // 기본 가중치
        }
    }

    private readonly List<SpawnableEntry> spawnableEntries = new List<SpawnableEntry>();
    private readonly Dictionary<EnemyHealth, int> activeMonsterCostMap = new Dictionary<EnemyHealth, int>();
    private int currentActiveBudget = 0;

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

    /// <summary>
    /// 등록된 프리팹들로부터 EnemyDataSO 정보를 읽어와 캐싱합니다.
    /// </summary>
    private void CacheEnemyPrefabs()
    {
        spawnableEntries.Clear();
        foreach (var prefab in enemyPrefabs)
        {
            if (prefab == null) continue;

            EnemyDataSO data = null;
            if (prefab.TryGetComponent(out EnemyHealth health))
            {
                data = health.EnemyData;
            }

            spawnableEntries.Add(new SpawnableEntry
            {
                Prefab = prefab,
                Data = data
            });
        }
    }

    public void RestartSpawner(int newDepth)
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);

        foreach (var pair in activeMonsterCostMap.ToList())
        {
            if (pair.Key != null && pair.Key.gameObject != null)
            {
                Destroy(pair.Key.gameObject);
            }
        }
        activeMonsterCostMap.Clear();
        currentActiveBudget = 0;

        CacheEnemyPrefabs();
        StartSpawning(newDepth);
    }

    private void StartSpawning(int depth)
    {
        spawnCoroutine = StartCoroutine(ContinuousSpawnRoutine(depth));
    }

    public int GetMaxActiveBudget(int depth) => baseBudget + (depth - 1) * budgetPerDepth;
    public int GetMaxMonsterCount(int depth) => baseMaxMonsterCount + (depth - 1) * monsterCountPerDepth;

    private IEnumerator ContinuousSpawnRoutine(int depth)
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            CleanStaleMonsters();

            int maxBudget = GetMaxActiveBudget(depth);
            int maxCount = GetMaxMonsterCount(depth);
            int availableBudget = maxBudget - currentActiveBudget;

            // 남은 예산이 있고, 화면 내 마릿수가 Depth 기준 상한 미만일 때 스폰 진행
            while (availableBudget > 0 && activeMonsterCostMap.Count < maxCount)
            {
                // 1. 현재 Depth 조건을 만족하고, 남은 예산 이하 Cost를 가진 프리팹 필터링
                var eligiblePool = spawnableEntries
                    .Where(e => e.Prefab != null && e.MinDepth <= depth && e.Cost <= availableBudget)
                    .ToList();

                if (eligiblePool.Count == 0) break;

                // 2. 티어 가속 공식 적용된 가중치 무작위 추첨
                SpawnableEntry selectedEntry = SelectByTierWeight(eligiblePool, depth);
                if (selectedEntry == null) break;

                // 3. 스폰 및 예산 차감
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
        if (totalWeight <= 0f) return pool[0];

        float randomPoint = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += weights[i];
            if (randomPoint <= cumulative)
            {
                return pool[i];
            }
        }

        return pool[0];
    }

    private void SpawnMonster(SpawnableEntry entry)
    {
        Vector3 spawnPos = GetRandomSpawnPosition();

        if (spawnEffectPrefab != null)
        {
            GameObject fx = Instantiate(spawnEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(fx, effectDuration);
        }

        GameObject enemyObj = Instantiate(entry.Prefab, spawnPos, Quaternion.identity);

        if (enemyObj.TryGetComponent(out EnemyHealth health))
        {
            activeMonsterCostMap[health] = entry.Cost;
        }
        else
        {
            EnemyHealth addedHealth = enemyObj.AddComponent<EnemyHealth>();
            activeMonsterCostMap[addedHealth] = entry.Cost;
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 origin = playerTransform != null ? playerTransform.position : Vector3.zero;
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

        return origin + new Vector3(randomDir.x, randomDir.y, 0f) * distance;
    }

    private void CleanStaleMonsters()
    {
        List<EnemyHealth> staleKeys = activeMonsterCostMap.Keys
            .Where(k => k == null || k.gameObject == null)
            .ToList();

        foreach (EnemyHealth stale in staleKeys)
        {
            currentActiveBudget = Mathf.Max(0, currentActiveBudget - activeMonsterCostMap[stale]);
            activeMonsterCostMap.Remove(stale);
        }
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (enemy != null && activeMonsterCostMap.TryGetValue(enemy, out int cost))
        {
            currentActiveBudget = Mathf.Max(0, currentActiveBudget - cost);
            activeMonsterCostMap.Remove(enemy);
        }
    }
}