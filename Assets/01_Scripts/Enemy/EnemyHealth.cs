using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;

    private float _currentHealth;

    private float _maxHealth;

    private bool _isDead = false;

    public event Action OnDamaged;
    public event Action OnDeath;

    private void Start()
    {
        _maxHealth = _enemyData.Stat.MaxHealth;
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        _currentHealth -= damage;

        OnDamaged?.Invoke();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
