using System;
using UnityEngine;

/// <summary>
/// 이번 판에서 처치한 적 수와 적에게 준 총 피해량을 센다. (결과창 표시용)
/// EnemyHealth의 전역 사망/피해 이벤트를 구독하며, 테스트용 DummyEnemy의 사망도 처치 수에 포함한다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyDeathTracker : MonoBehaviour
{
    private int _killCount;
    private float _totalDamage;

    /// <summary>적(EnemyHealth)이 죽었을 때 발생한다.</summary>
    public event Action<EnemyHealth> EnemyKilled;

    public int KillCount => _killCount;

    public float TotalDamage => _totalDamage;

    private void OnEnable()
    {
        EnemyHealth.OnDeath += HandleEnemyDeath;
        EnemyHealth.OnDamageDealt += HandleDamageDealt;
        DummyEnemy.OnEnemyDied += HandleDummyEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleEnemyDeath;
        EnemyHealth.OnDamageDealt -= HandleDamageDealt;
        DummyEnemy.OnEnemyDied -= HandleDummyEnemyDied;
    }

    private void HandleEnemyDeath(EnemyHealth enemy)
    {
        _killCount++;
        SafeEventNotifier.Notify(EnemyKilled, enemy);
    }

    private void HandleDamageDealt(EnemyHealth enemy, float damage)
    {
        _totalDamage += damage;
    }

    private void HandleDummyEnemyDied(DummyEnemy enemy)
    {
        // EnemyHealth가 함께 붙은 적은 위에서 이미 센다.
        if (enemy != null && enemy.GetComponent<EnemyHealth>() == null)
        {
            _killCount++;
        }
    }
}
