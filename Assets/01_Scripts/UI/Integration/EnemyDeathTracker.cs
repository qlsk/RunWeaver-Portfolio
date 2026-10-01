using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬의 적(EnemyHealth)을 주기적으로 찾아 사망을 구독하고, 처치 수를 센다.
/// 적 사망을 전역으로 알려주는 이벤트가 아직 없어 UI 연동을 위해 임시로 만든 컴포넌트이다.
/// 적 쪽에 전역 이벤트가 생기면 그 이벤트를 구독하도록 교체한다.
/// </summary>
[DisallowMultipleComponent]
public class EnemyDeathTracker : MonoBehaviour
{
    [Tooltip("새로 생긴 적을 찾는 간격 (초)")]
    [SerializeField, Min(0.05f)] private float _scanInterval = 0.25f;

    private readonly Dictionary<EnemyHealth, Action> _subscriptions = new();
    private readonly List<EnemyHealth> _removeBuffer = new();
    private float _nextScanTime;
    private int _killCount;

    /// <summary>적(EnemyHealth)이 죽었을 때 발생한다.</summary>
    public event Action<EnemyHealth> EnemyKilled;

    public int KillCount => _killCount;

    private void OnEnable()
    {
        DummyEnemy.OnEnemyDied += HandleDummyEnemyDied;
    }

    private void OnDisable()
    {
        DummyEnemy.OnEnemyDied -= HandleDummyEnemyDied;
    }

    private void Update()
    {
        if (Time.time < _nextScanTime)
        {
            return;
        }

        _nextScanTime = Time.time + _scanInterval;
        RemoveDestroyedEnemies();

        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (_subscriptions.ContainsKey(enemy) || enemy.IsDead)
            {
                continue;
            }

            Action handler = () => HandleEnemyDeath(enemy);
            enemy.OnDeath += handler;
            _subscriptions.Add(enemy, handler);
        }
    }

    private void OnDestroy()
    {
        foreach ((EnemyHealth enemy, Action handler) in _subscriptions)
        {
            if (enemy != null)
            {
                enemy.OnDeath -= handler;
            }
        }

        _subscriptions.Clear();
    }

    private void HandleEnemyDeath(EnemyHealth enemy)
    {
        _killCount++;
        if (_subscriptions.TryGetValue(enemy, out Action handler))
        {
            enemy.OnDeath -= handler;
            _subscriptions.Remove(enemy);
        }

        SafeEventNotifier.Notify(EnemyKilled, enemy);
    }

    private void HandleDummyEnemyDied(DummyEnemy enemy)
    {
        // 처치 미션 테스트용 DummyEnemy도 결과창 처치 수에 포함한다. (EnemyHealth가 함께 있으면 위에서 이미 센다)
        if (enemy != null && enemy.GetComponent<EnemyHealth>() == null)
        {
            _killCount++;
        }
    }

    private void RemoveDestroyedEnemies()
    {
        _removeBuffer.Clear();
        foreach (EnemyHealth enemy in _subscriptions.Keys)
        {
            if (enemy == null)
            {
                _removeBuffer.Add(enemy);
            }
        }

        foreach (EnemyHealth enemy in _removeBuffer)
        {
            _subscriptions.Remove(enemy);
        }
    }
}
