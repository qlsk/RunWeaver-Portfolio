using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private float _currentHealth = 10f;

    private float _maxHealth;
    private bool _isDead = false;

    public bool IsDead => _isDead;
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public EnemyDataSO EnemyData => _enemyData; // 스포너가 읽어갈 수 있도록 프로퍼티 제공

    public event Action OnDamaged;
    public static event Action<EnemyHealth> OnDeath;
    public static event Action<EnemyHealth, float> OnDamageDealt;

    private void Start()
    {
        if (_enemyData != null && _enemyData.Stat != null)
        {
            _maxHealth = _enemyData.Stat.MaxHealth;
        }
        else
        {
            _maxHealth = _currentHealth > 0f ? _currentHealth : 10f;
        }

        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        ApplyDamage(damage);
    }

    public float ApplyDamage(float damage)
    {
        if (_isDead || damage <= 0f) return 0f;
        float actualDamage = Mathf.Min(damage, Mathf.Max(0f, _currentHealth));
        _currentHealth -= actualDamage;
        bool killed = _currentHealth <= 0f;
        if (killed) _isDead = true;

        OnDamaged?.Invoke();
        OnDamageDealt?.Invoke(this, actualDamage);

        if (killed)
        {
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }
        return actualDamage;
    }
}