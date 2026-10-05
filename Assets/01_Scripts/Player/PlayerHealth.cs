using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("최대 체력")]
    [SerializeField] private float _maxHealth = 100f;
    public float MaxHealth => _maxHealth;
    [Header("현재 체력")]
    [SerializeField] private float _currentHealth;
    public float CurrentHealth => _currentHealth;

    [Header("피격 무적 지속 시간")]
    [SerializeField] private float _invincibilityDuration = 0.5f;

    public event Action Died;

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

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    // 피격
    public void TakeDamage(float damage)
    {
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
        if (_currentHealth - damage <= 0f)
        {
            _currentHealth = 0f;
            Death();
        }
        else
        {
            _currentHealth -= damage;
        }

    }

    private IEnumerator TakeDamageCoroutine(float damage)
    {
        _hitInvincibility = true;
        ApplyDamage(damage);
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
