using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("생성기 및 데이터 연동")]
    [SerializeField] private StageGenerator stageGenerator;
    [SerializeField] private int currentStageDepth = 1;

    [Header("기본 더미 프리팹 (MonsterDataSO 프리팹 미설정 시 폴백용)")]
    [SerializeField] private GameObject defaultEnemyPrefab;
    [SerializeField] private GameObject spawnEffectPrefab;

    [Header("스폰 거리 및 연출 설정")]
    [SerializeField] private float minSpawnDistance = 4f;  // 플레이어 기준 최소 거리
    [SerializeField] private float maxSpawnDistance = 7f;  // 플레이어 기준 최대 거리
    [SerializeField] private float effectDuration = 1f;    // 스폰 이펙트 연출 시간

    private Transform playerTransform;
    private StageRuntimeData currentStageData;
    private readonly List<GameObject> activeMonsters = new List<GameObject>();
    private Coroutine currentSpawnCoroutine;

    private void OnEnable()
    {
        DummyEnemy.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        DummyEnemy.OnEnemyDied -= HandleEnemyDied;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        if (stageGenerator == null)
        {
            stageGenerator = FindAnyObjectByType<StageGenerator>();
        }

        InitAndStartSpawning(currentStageDepth);
    }

    /// <summary>
    /// 다음 스테이지(Depth) 진입 시 스포너 데이터를 갱신하고 스폰 루틴을 재시작합니다.
    /// </summary>
    public void RestartSpawner(int newDepth)
    {
        currentStageDepth = newDepth;

        if (currentSpawnCoroutine != null)
        {
            StopCoroutine(currentSpawnCoroutine);
        }

        activeMonsters.RemoveAll(m => m == null);
        foreach (var enemy in activeMonsters)
        {
            if (enemy != null) Destroy(enemy);
        }
        activeMonsters.Clear();

        InitAndStartSpawning(currentStageDepth);
    }

    private void InitAndStartSpawning(int depth)
    {
        if (stageGenerator != null)
        {
            currentStageData = stageGenerator.GenerateStageData(depth);
        }

        if (currentStageData == null)
        {
            Debug.LogWarning("[MonsterSpawner] StageRuntimeData를 불러오지 못했습니다.");
        }

        currentSpawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private void HandleEnemyDied(DummyEnemy enemy)
    {
        if (enemy != null && activeMonsters.Contains(enemy.gameObject))
        {
            activeMonsters.Remove(enemy.gameObject);
        }
        activeMonsters.RemoveAll(item => item == null);
    }

    private IEnumerator SpawnRoutine()
    {
        int initialCount = currentStageData != null ? currentStageData.initialSpawnCount : 8;
        int maxActive = currentStageData != null ? currentStageData.maxActiveMonsters : 30;
        float interval = currentStageData != null ? currentStageData.spawnInterval : 5f;
        int batchCount = currentStageData != null ? currentStageData.spawnBatchCount : 5;

        yield return SpawnBatch(initialCount, maxActive);

        while (true)
        {
            yield return new WaitForSeconds(interval);

            activeMonsters.RemoveAll(item => item == null);

            if (activeMonsters.Count < maxActive)
            {
                int spawnableCount = Mathf.Min(batchCount, maxActive - activeMonsters.Count);
                yield return SpawnBatch(spawnableCount, maxActive);
            }
        }
    }

    private IEnumerator SpawnBatch(int count, int maxLimit)
    {
        for (int i = 0; i < count; i++)
        {
            activeMonsters.RemoveAll(item => item == null);
            if (activeMonsters.Count >= maxLimit) break;

            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject targetPrefab = SelectMonsterPrefab();

            if (targetPrefab != null)
            {
                StartCoroutine(SpawnSingleMonsterWithEffect(spawnPos, targetPrefab));
            }

            yield return new WaitForSeconds(0.15f);
        }
    }

    private IEnumerator SpawnSingleMonsterWithEffect(Vector3 spawnPosition, GameObject prefabToSpawn)
    {
        if (spawnEffectPrefab != null)
        {
            GameObject fx = Instantiate(spawnEffectPrefab, spawnPosition, Quaternion.identity);
            Destroy(fx, effectDuration);
            yield return new WaitForSeconds(effectDuration);
        }

        if (prefabToSpawn != null)
        {
            GameObject enemyObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            activeMonsters.Add(enemyObj);
        }
    }

    private GameObject SelectMonsterPrefab()
    {
        if (currentStageData != null && currentStageData.selectedMonsters.Count > 0)
        {
            float randomVal = Random.value;
            float cumulativeRatio = 0f;

            for (int i = 0; i < currentStageData.selectedMonsters.Count; i++)
            {
                cumulativeRatio += currentStageData.monsterSpawnRatios[i];
                if (randomVal <= cumulativeRatio)
                {
                    var data = currentStageData.selectedMonsters[i];
                    if (data != null && data.monsterPrefab != null)
                    {
                        return data.monsterPrefab;
                    }
                }
            }
        }

        return defaultEnemyPrefab;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (playerTransform == null) return Vector3.zero;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);

        return playerTransform.position + new Vector3(randomDir.x, randomDir.y, 0) * randomDist;
    }
}