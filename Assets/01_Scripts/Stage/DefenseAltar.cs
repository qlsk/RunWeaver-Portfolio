using System;
using UnityEngine;

/// <summary>
/// 제단 디펜스 미션에서 지켜야 하는 중앙 제단 오브젝트 컴포넌트.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DefenseAltar : MonoBehaviour
{
    [Header("제단 체력 설정")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("테스트 편의 기능")]
    [Tooltip("에디터 테스트 시 마우스 클릭(좌클릭/우클릭)이나 단축키로 제단에 피해를 줄 수 있습니다.")]
    [SerializeField] private bool allowClickDamageForTest = true;
    [SerializeField] private float testDamageAmount = 25f;
    [Tooltip("이 키를 누르면 마우스 조작 없이도 제단에 즉시 피해가 들어갑니다.")]
    [SerializeField] private KeyCode testDamageKey = KeyCode.K;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDestroyed => currentHealth <= 0f;

    public event Action<float, float> OnHealthChanged;
    public event Action OnAltarDestroyed;

    private void Awake()
    {
        currentHealth = maxHealth;

        // 마우스 클릭 감지를 위해 Collider2D가 없으면 자동 추가
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        // 1. 키보드 단축키(K)로 즉시 피해 테스트 가능
        if (allowClickDamageForTest && Input.GetKeyDown(testDamageKey))
        {
            Debug.Log($"[DefenseAltar] 단축키({testDamageKey}) 입력으로 제단 피해 적용!");
            TakeDamage(testDamageAmount);
        }
    }

    /// <summary>
    /// 마우스 좌클릭 시 즉시 제단 피격 (더미 몬스터와 동일)
    /// </summary>
    private void OnMouseDown()
    {
        if (allowClickDamageForTest)
        {
            TakeDamage(testDamageAmount);
        }
    }

    /// <summary>
    /// 마우스 우클릭 시에도 피격 지원
    /// </summary>
    private void OnMouseOver()
    {
        if (allowClickDamageForTest && Input.GetMouseButtonDown(1))
        {
            TakeDamage(testDamageAmount);
        }
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
