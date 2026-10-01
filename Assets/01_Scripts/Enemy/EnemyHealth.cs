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
    // 실제로 깎인 체력만큼 전달한다. (결과창 총 피해량 집계용)
    public static event Action<EnemyHealth, float> OnDamageDealt;

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
        OnDamageDealt?.Invoke(this, actualDamage);

        if (killed)
        {
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }
        return actualDamage;
    }

}
