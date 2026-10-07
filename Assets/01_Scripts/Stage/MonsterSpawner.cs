using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class MonsterSpawner : MonoBehaviour
{
    private static MonsterSpawner s_instance;

    public static MonsterSpawner Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = FindAnyObjectByType<MonsterSpawner>();
            }
            return s_instance;
        }
    }

    [Header("몬스터 프리팹 데이터베이스 (프리팹 직접 등록)")]
    [SerializeField, FormerlySerializedAs("enemyPrefabs")] private List<GameObject> _enemyPrefabs = new List<GameObject>();

    [Header("예산(Budget) 스케일링 설정")]
    [SerializeField, FormerlySerializedAs("baseBudget")] private int _baseBudget = 20;
    [SerializeField, FormerlySerializedAs("budgetPerDepth")] private int _budgetPerDepth = 10;

    [Header("최대 마릿수(Count) 스케일링 설정")]
    [SerializeField, FormerlySerializedAs("baseMaxMonsterCount")] private int _baseMaxMonsterCount = 15;
    [SerializeField, FormerlySerializedAs("monsterCountPerDepth")] private int _monsterCountPerDepth = 5;

    [Header("스폰 주기 및 티어 가속 계수")]
    [SerializeField, FormerlySerializedAs("spawnInterval")] private float _spawnInterval = 0.5f;
    [SerializeField, FormerlySerializedAs("tierGrowthMultiplier")] private float _tierGrowthMultiplier = 5f;

    [Header("스폰 거리 및 연출")]
    [SerializeField, FormerlySerializedAs("minSpawnDistance")] private float _minSpawnDistance = 8f;
    [SerializeField, FormerlySerializedAs("maxSpawnDistance")] private float _maxSpawnDistance = 12f;
    [SerializeField, FormerlySerializedAs("spawnEffectPrefab")] private GameObject _spawnEffectPrefab;
    [SerializeField, FormerlySerializedAs("effectDuration")] private float _effectDuration = 1f;

    private Transform _playerTransform;
    private Coroutine _spawnCoroutine;

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

    private readonly List<SpawnableEntry> _spawnableEntries = new List<SpawnableEntry>();
    private readonly Dictionary<EnemyHealth, int> _activeMonsterCostMap = new Dictionary<EnemyHealth, int>();
    private int _currentActiveBudget = 0;

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
        if (playerObj != null) _playerTransform = playerObj.transform;

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
        _spawnableEntries.Clear();

        _enemyPrefabs = _enemyPrefabs.Where(p => p != null).ToList();

        if (_enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("[MonsterSpawner] enemyPrefabs에 유효한 프리팹이 없습니다! Project 창의 02_Prefabs/Enemy/DefaultEnemy 프리팹을 인스펙터에 끌어다 놓아주세요.");
            return;
        }

        int validCount = 0;
        foreach (var prefab in _enemyPrefabs)
        {
            EnemyDataSO data = null;
            if (prefab.TryGetComponent(out EnemyHealth health) || (health = prefab.GetComponentInChildren<EnemyHealth>()) != null)
            {
                data = health.EnemyData;
            }

            _spawnableEntries.Add(new SpawnableEntry
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
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        _spawnCoroutine = StartCoroutine(ContinuousSpawnRoutine(depth));
    }

    public void StopSpawning()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        foreach (var pair in _activeMonsterCostMap.ToList())
        {
            if (pair.Key != null && pair.Key.gameObject != null)
            {
                Destroy(pair.Key.gameObject);
            }
        }
        _activeMonsterCostMap.Clear();
        _currentActiveBudget = 0;

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

    public int GetMaxActiveBudget(int depth) => _baseBudget + (depth - 1) * _budgetPerDepth;
    public int GetMaxMonsterCount(int depth) => _baseMaxMonsterCount + (depth - 1) * _monsterCountPerDepth;

    private IEnumerator ContinuousSpawnRoutine(int depth)
    {
        while (true)
        {
            yield return new WaitForSeconds(_spawnInterval);

            CleanStaleMonsters();

            if (_spawnableEntries.Count == 0)
            {
                CacheEnemyPrefabs();
                if (_spawnableEntries.Count == 0) continue;
            }

            int maxBudget = GetMaxActiveBudget(depth);
            int maxCount = GetMaxMonsterCount(depth);
            int availableBudget = maxBudget - _currentActiveBudget;

            while (availableBudget > 0 && _activeMonsterCostMap.Count < maxCount)
            {
                // ★ 현재 Depth 조건(MinDepth <= depth) 및 남은 예산(Cost <= availableBudget)을 충족하는 풀만 추출 ★
                var eligiblePool = _spawnableEntries
                    .Where(e => e.Prefab != null && e.MinDepth <= depth && e.Cost <= availableBudget)
                    .ToList();

                // 조건에 맞는 몬스터가 없으면 강제 대체 풀을 사용하지 않고 이번 스폰 시도를 중단(대기)
                if (eligiblePool.Count == 0) break;

                SpawnableEntry selectedEntry = SelectByTierWeight(eligiblePool, depth);
                if (selectedEntry == null) break;

                if (SpawnMonster(selectedEntry))
                {
                    _currentActiveBudget += selectedEntry.Cost;
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
            .Select(e => e.GetCalculatedWeight(depth, _tierGrowthMultiplier))
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

        if (_playerTransform == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) _playerTransform = pObj.transform;
            else return false;
        }

        Vector2 spawnPos = GetRandomSpawnPosition();

        GameObject spawned = Instantiate(entry.Prefab, spawnPos, Quaternion.identity);

        EnemyHealth health = spawned.GetComponent<EnemyHealth>() ?? spawned.GetComponentInChildren<EnemyHealth>();
        if (health != null)
        {
            if (_spawnEffectPrefab != null)
            {
                GameObject fx = Instantiate(_spawnEffectPrefab, spawnPos, Quaternion.identity);
                Destroy(fx, _effectDuration);
            }

            _activeMonsterCostMap[health] = entry.Cost;
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
        Vector2 center = _playerTransform != null ? (Vector2)_playerTransform.position : Vector2.zero;
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float distance = UnityEngine.Random.Range(_minSpawnDistance, _maxSpawnDistance);

        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (enemy != null && _activeMonsterCostMap.TryGetValue(enemy, out int cost))
        {
            _currentActiveBudget = Mathf.Max(0, _currentActiveBudget - cost);
            _activeMonsterCostMap.Remove(enemy);
        }
    }

    private void CleanStaleMonsters()
    {
        var deadKeys = _activeMonsterCostMap.Keys.Where(k => k == null || k.gameObject == null).ToList();
        foreach (var key in deadKeys)
        {
            _currentActiveBudget = Mathf.Max(0, _currentActiveBudget - _activeMonsterCostMap[key]);
            _activeMonsterCostMap.Remove(key);
        }
    }
}
