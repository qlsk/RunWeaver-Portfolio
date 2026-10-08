using System;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyDataSO _enemyData;
    [SerializeField] private float _currentHealth = 10f;
    [SerializeField] private MMF_Player _damageFeedbacks;

    private float _maxHealth;
    private bool _isDead = false;
    private float _defenseMultiplier = 1f;
    private readonly Dictionary<int, float> _defenseModifiers = new();

    public bool IsDead => _isDead;
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public EnemyDataSO EnemyData => _enemyData; // 스포너가 읽어갈 수 있도록 프로퍼티 제공
    public float Defense => (_enemyData != null && _enemyData.Stat != null ? _enemyData.Stat.Defense : 0f) * _defenseMultiplier;

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

    /// 방어력을 반영한 피해를 체력에 적용하고 실제 피해량과 사망 이벤트를 전달한다.
    public float ApplyDamage(float damage)
    {
        if (_isDead || damage <= 0f) return 0f;
        float mitigatedDamage = damage * 100f / (100f + Defense);
        float actualDamage = Mathf.Min(mitigatedDamage, Mathf.Max(0f, _currentHealth));
        _currentHealth -= actualDamage;
        bool killed = _currentHealth <= 0f;
        if (killed) _isDead = true;

        _damageFeedbacks?.PlayFeedbacks(transform.position);
        OnDamaged?.Invoke();
        OnDamageDealt?.Invoke(this, actualDamage);

        if (killed)
        {
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        Debug.Log($"HP reduced by {actualDamage}. Current HP: {_currentHealth}");
        return actualDamage;
    }

    /// 출처별 방어력 배율을 등록하고 가장 강한 방어력 감소를 반영한다.
    public void AddDefenseModifier(int sourceId, float multiplier)
    {
        _defenseModifiers[sourceId] = multiplier;
        RefreshDefenseMultiplier();
    }

    /// 해당 출처의 방어력 감소만 해제하고 남아 있는 배율을 반영한다.
    public void RemoveDefenseModifier(int sourceId)
    {
        _defenseModifiers.Remove(sourceId);
        RefreshDefenseMultiplier();
    }

    /// 방어력 감소를 중첩 곱하지 않고 등록된 배율 중 가장 낮은 값을 선택한다.
    private void RefreshDefenseMultiplier()
    {
        _defenseMultiplier = 1f;
        foreach (float multiplier in _defenseModifiers.Values)
            _defenseMultiplier = Mathf.Min(_defenseMultiplier, multiplier);
    }
}
