using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("스폰 대상 설정")]
    [SerializeField] private GameObject dummyEnemyPrefab;    // 더미 몬스터 프리팹
    [SerializeField] private GameObject spawnEffectPrefab;   // 스폰 이펙트 (없으면 생략 가능)

    [Header("스폰 조건 설정")]
    [SerializeField] private float initialDelay = 5f;        // 입장 후 5초 대기
    [SerializeField] private float minSpawnDistance = 4f;    // 플레이어 최소 거리
    [SerializeField] private float maxSpawnDistance = 7f;    // 플레이어 최대 거리
    [SerializeField] private float effectDuration = 1f;      // 이펙트 연출 시간
    [SerializeField] private int totalSpawnCount = 5;        // 생성할 더미 몬스터 수

    private Transform playerTransform;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        StartCoroutine(SpawnSequenceRoutine());
    }

    private IEnumerator SpawnSequenceRoutine()
    {
        // 1. 스테이지 입장 후 5초 대기
        yield return new WaitForSeconds(initialDelay);

        // 2. 설정된 수만큼 무작위 위치에 스폰
        for (int i = 0; i < totalSpawnCount; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            StartCoroutine(SpawnSingleMonsterWithEffect(spawnPos));
            yield return new WaitForSeconds(0.3f); // 스폰 간격
        }
    }

    private IEnumerator SpawnSingleMonsterWithEffect(Vector3 spawnPosition)
    {
        // 하데스 방식 연출 1: 스폰 위치에 이펙트 미리 생성
        if (spawnEffectPrefab != null)
        {
            GameObject fx = Instantiate(spawnEffectPrefab, spawnPosition, Quaternion.identity);
            Destroy(fx, effectDuration);
        }

        // 연출 대기
        yield return new WaitForSeconds(effectDuration);

        // 하데스 방식 연출 2: 이펙트 끝난 후 실제 몬스터 생성
        if (dummyEnemyPrefab != null)
        {
            Instantiate(dummyEnemyPrefab, spawnPosition, Quaternion.identity);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (playerTransform == null) return Vector3.zero;

        Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
        float randomDist = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);

        return playerTransform.position + new Vector3(randomDir.x, randomDir.y, 0) * randomDist;
    }
}
