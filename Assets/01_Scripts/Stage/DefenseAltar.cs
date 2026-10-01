using System;
using UnityEngine;

public class DefenseAltar : MonoBehaviour
{
    public static event Action OnAltarDestroyed;

    [Header("제단 체력 설정")]
    [SerializeField] private float maxHp = 100f;
    private float currentHp;

    public bool IsDestroyed => currentHp <= 0f;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    private void Start()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed) return;

        currentHp -= damage;
        Debug.Log($"[DefenseAltar] 제단 피격! 남은 HP: {currentHp}/{maxHp}");

        if (currentHp <= 0f)
        {
            currentHp = 0f;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[DefenseAltar] 제단 파괴!");
        OnAltarDestroyed?.Invoke();
        Destroy(gameObject);
    }

    // 테스트용 제단 피격 및 파괴 입력
    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1)) // 마우스 우클릭 시 데미지 테스트
        {
            TakeDamage(20f);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) // K키 누르면 제단 즉시 파괴 테스트
        {
            TakeDamage(maxHp);
        }
    }
}