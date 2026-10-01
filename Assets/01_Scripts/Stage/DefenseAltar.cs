using System;
using UnityEngine;

public class DefenseAltar : MonoBehaviour
{
    public static event Action OnAltarDestroyed;

    [Header("제단 체력 설정")]
    [SerializeField] private float maxHp = 100f;
    private float currentHp = 100f;
    private bool isInitialized = false;

    [Header("테스트/디버그 입력 (기본 비활성화)")]
    [Tooltip("체크 시에만 마우스 우클릭 및 단축키 피격 테스트가 동작합니다.")]
    [SerializeField] private bool allowTestInputs = false;

    public bool IsDestroyed => isInitialized && currentHp <= 0f;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    private void Awake()
    {
        currentHp = maxHp;
        isInitialized = true;
    }

    private void Start()
    {
        if (!isInitialized || currentHp <= 0f)
        {
            currentHp = maxHp;
            isInitialized = true;
        }
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

    // 마우스 우클릭 테스트 (실사용 중 실수 클릭 방지를 위해 allowTestInputs 가 true 일 때만 동작)
    private void OnMouseOver()
    {
        if (allowTestInputs && Input.GetMouseButtonDown(1))
        {
            TakeDamage(20f);
        }
    }

    // K키 즉시 파괴 테스트 (실사용 중 실수 입력 방지를 위해 allowTestInputs 가 true 일 때만 동작)
    private void Update()
    {
        if (allowTestInputs && Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(maxHp);
        }
    }
}