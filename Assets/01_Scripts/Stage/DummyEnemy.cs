using System;
using UnityEngine;

public class DummyEnemy : MonoBehaviour
{
    public static event Action<DummyEnemy> OnEnemyDied;

    [Header("몬스터 속성")]
    [SerializeField] private bool isElite = false;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stoppingDistance = 0.5f;

    [Header("타겟 설정 (비워둘 경우 Player 태그 자동 탐색)")]
    [SerializeField] private Transform targetTransform;

    public bool IsElite => isElite;

    private void Start()
    {
        FindTarget();
    }

    private void Update()
    {
        // 타겟이 유효하지 않다면 재탐색
        if (targetTransform == null)
        {
            FindTarget();
        }

        // 타겟 방향으로 이동
        if (targetTransform != null)
        {
            float distance = Vector2.Distance(transform.position, targetTransform.position);
            if (distance > stoppingDistance)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetTransform.position,
                    moveSpeed * Time.deltaTime
                );
            }
        }
    }

    /// <summary>
    /// 플레이어(Player 태그) 또는 디펜스 제단(DefenseAltar)을 타겟으로 우선 지정
    /// </summary>
    private void FindTarget()
    {
        // 1. Player 태그 탐색
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetTransform = playerObj.transform;
            return;
        }

        // 2. Player가 없을 경우 씬의 제단(DefenseAltar) 탐색
        DefenseAltar altar = FindAnyObjectByType<DefenseAltar>();
        if (altar != null)
        {
            targetTransform = altar.transform;
        }
    }

    // 마우스 좌클릭 시 피격/사망 테스트 (기존 미션 테스트용)
    private void OnMouseDown()
    {
        Die();
    }

    public void Die()
    {
        Debug.Log($"[DummyEnemy] 더미 몬스터 사망: {gameObject.name} (IsElite: {isElite})");
        OnEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }
}