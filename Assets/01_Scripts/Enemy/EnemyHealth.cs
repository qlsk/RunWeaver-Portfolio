using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;

    [SerializeField] private float _currentHealth;

    private float _maxHealth;

    private bool _isDead = false;
    public bool IsDead => _isDead;

    public event Action OnDamaged;
    public static event Action<EnemyHealth> OnDeath;

    private void Start()
    {
        _maxHealth = _enemyData.Stat.MaxHealth;
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

        if (killed)
        {
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        Debug.Log($"HP reduced by {actualDamage}. Current HP: {_currentHealth}");
        return actualDamage;
    }
}