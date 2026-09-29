using System;
using UnityEngine;

/// <summary>
/// 제단 디펜스 미션에서 지켜야 하는 중앙 제단 오브젝트 컴포넌트.
/// </summary>
public class DefenseAltar : MonoBehaviour
{
    [Header("제단 체력 설정")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("테스트 편의 기능")]
    [Tooltip("에디터 테스트 시 마우스 우클릭으로 제단에 피해를 줄 수 있습니다.")]
    [SerializeField] private bool allowClickDamageForTest = true;
    [SerializeField] private float testDamageAmount = 25f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDestroyed => currentHealth <= 0f;

    public event Action<float, float> OnHealthChanged;
    public event Action OnAltarDestroyed;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 적의 공격 또는 투사체로부터 피해를 입을 때 호출
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (IsDestroyed) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"[DefenseAltar] 제단 피격! 피해량: {damage}, 남은 체력: {currentHealth} / {maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Debug.LogError("[DefenseAltar] ✖ 제단이 파괴되었습니다! ✖");
            OnAltarDestroyed?.Invoke();
        }
    }

    private void OnMouseOver()
    {
        // 테스트 편의: 우클릭 시 제단에 데미지 적용
        if (allowClickDamageForTest && Input.GetMouseButtonDown(1))
        {
            TakeDamage(testDamageAmount);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 더미 몬스터와 충돌했을 때 제단에 피해를 주고 몬스터 소멸
        if (collision.TryGetComponent<DummyEnemy>(out var dummyEnemy))
        {
            TakeDamage(20f);
            dummyEnemy.Die();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsDestroyed ? Color.red : Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 1.5f, 0f));
    }
}
