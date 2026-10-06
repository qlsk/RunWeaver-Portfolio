using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// 생성 위치의 잔불 범위에 일정 주기로 피해를 적용하고 지속 시간이 끝나면 제거한다.
public class EmberArea : MonoBehaviour
{
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime Settings")] private float _tickDamage;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime Settings")] private float _radius;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime Settings")] private float _tickInterval;

    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private float _remainingDuration;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private float _tickTimer;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private bool _isInitialized;

    public void Initialize(float tickDamage, float radius, float tickInterval, float duration)
    {
        _tickDamage = tickDamage;
        _radius = radius;
        _tickInterval = tickInterval;
        _remainingDuration = duration;
        _tickTimer = 0f;
        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        float elapsed = Mathf.Min(Time.deltaTime, _remainingDuration);
        _remainingDuration = Mathf.Max(0f, _remainingDuration - elapsed);
        _tickTimer += elapsed;

        while (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyTickDamage();
        }

        if (_remainingDuration <= 0f)
            Destroy(gameObject);
    }

    private void ApplyTickDamage()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _radius);
        HashSet<EnemyHealth> enemies = new();
        foreach (Collider2D collider in colliders)
        {
            if (!collider.TryGetComponent(out EnemyHealth enemy))
                continue;
            enemies.Add(enemy);
        }

        IEnumerable<EnemyHealth> damageableEnemies =
            enemies.Where(enemy => enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled);
        foreach (EnemyHealth enemy in damageableEnemies)
        {
            enemy.TakeDamage(_tickDamage);
        }
    }
}