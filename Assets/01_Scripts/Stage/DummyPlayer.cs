using System;
using UnityEngine;

public class DummyPlayer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [Header("사망 테스트 편의 기능")]
    [Tooltip("테스트 시 이 키를 누르면 플레이어가 사망 처리됩니다.")]
    [SerializeField] private KeyCode testDieKey = KeyCode.L;

    public static event Action OnDummyPlayerDied;

    private bool isDead = false;

    private void Update()
    {
        if (isDead) return;

        // 테스트 편의: L키 누르면 플레이어 사망
        if (Input.GetKeyDown(testDieKey))
        {
            Die();
            return;
        }

        // 간단한 2D 방향키 이동
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector3 dir = new Vector3(moveX, moveY, 0).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("[DummyPlayer] ✖ 플레이어 사망 처리됨! ✖");
        OnDummyPlayerDied?.Invoke();
    }
}
