using System;
using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("피격 피드백")]
    [SerializeField] private MMF_Player _damageFeedbacks;

    [Header("최대 체력")]
    [SerializeField] private float _maxHealth = 100f;
    public float MaxHealth => _maxHealth;
    [Header("현재 체력")]
    [SerializeField] private float _currentHealth;
    public float CurrentHealth => _currentHealth;

    [Header("피격 무적 지속 시간")]
    [SerializeField] private float _invincibilityDuration = 0.5f;

    /// <summary>보상 특성용: 피격 무적 지속 시간 외부 조정.</summary>
    public float InvincibilityDuration
    {
        get => _invincibilityDuration;
        set => _invincibilityDuration = Mathf.Max(0f, value);
    }

    private Shield _shield;

    public event Action Died;

    /// <summary>보상 특성용: 실제 피해 확정 통지 (쉴드 소모·무적 회피분은 나가지 않음).</summary>
    public event Action<float> Damaged;

    /// <summary>보상 특성용 피해 배율 (기본 1). 받는 피해에 곱해진다.</summary>
    public float DamageTakenMultiplier { get; set; } = 1f;

    private bool _isDead = false;
    public bool IsDead => _isDead;

    private bool _dashInvincibility = false;

    public void SetDashInvincibility(bool invincibility)
    {
        _dashInvincibility = invincibility;
    }

    private bool _hitInvincibility = false;

    // private bool _canTakeDamage = true;
    //
    // public void SetCanTakeDamage(bool canTakeDamage)
    // {
    //     _canTakeDamage = canTakeDamage;
    // }

    private void Start()
    {
        _shield = GetComponent<Shield>();
        if (_damageFeedbacks != null)
            _ = MMTimeManager.Instance;
    }

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    // 피격
    public void TakeDamage(float damage)
    {
        Debug.Log($"Current Shield = {_shield.CurrentShield}");
        if (_isDead || damage <= 0f) return;
        if (_hitInvincibility || _dashInvincibility) return;
        StartCoroutine(TakeDamageCoroutine(damage));
    }

    /// <summary>지속 피해는 피격/대시 무적을 무시하고 새 피격 무적을 만들지 않는다.</summary>
    public void TakeDamageOverTime(float damage)
    {
        if (_isDead || damage <= 0f) return;
        ApplyDamage(damage);
    }

    private void ApplyDamage(float damage)
    {
        float finalDamage = Mathf.Max(0f, damage) * DamageTakenMultiplier;
        if (finalDamage <= 0f)
            return;

        _damageFeedbacks?.PlayFeedbacks(transform.position);
        Damaged?.Invoke(finalDamage);
        if (_currentHealth - finalDamage <= 0f)
        {
            _currentHealth = 0f;
            Death();
        }
        else
        {
            _currentHealth -= finalDamage;
        }
    }

    private IEnumerator TakeDamageCoroutine(float damage)
    {
        _hitInvincibility = true;
        if (_shield.CurrentShield > 0)
        {
            _shield.DestroyShield();
        }
        else
        {
            ApplyDamage(damage);
        }

        yield return new WaitForSeconds(_invincibilityDuration);
        _hitInvincibility = false;
    }

    // 체력 회복
    public void Heal(float healAmount)
    {
        if (_isDead || healAmount <= 0f) return;

        _currentHealth = _currentHealth + healAmount > _maxHealth ? _maxHealth : _currentHealth + healAmount;
    }

    /// <summary>
    /// 무적 여부와 상관없이 즉시 사망 처리한다. (일시정지 메뉴의 탐사 포기 등)
    /// </summary>
    public void Kill()
    {
        if (_isDead) return;
        Death();
    }

    // 사망
    private void Death()
    {
        if (_isDead) return;
        _isDead = true;
        _currentHealth = 0f;

        Died?.Invoke();
    }
}
