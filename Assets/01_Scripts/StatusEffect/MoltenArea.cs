using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// 용암 범위에 주기적으로 피해를 적용하고 범위 내 적의 이동 속도를 낮춘다.
public class MoltenArea : MonoBehaviour
{
    [FoldoutGroup("Settings"), SerializeField] private float _rangeCheckInterval = 0.1f;

    private float _tickDamage;
    private float _radius;
    private float _tickInterval;
    private float _moveSpeedMultiplier;

    private int _sourceId;
    private float _remainingDuration;
    private float _tickTimer;
    private float _rangeCheckTimer;
    private bool _isInitialized;
    private readonly HashSet<EnemyStatusEffectController> _affectedStatusEffects = new();

    public void Initialize(float tickDamage, float radius, float tickInterval, float duration,
        float moveSpeedMultiplier)
    {
        ClearSlowModifiers();

        _tickDamage = tickDamage;
        _radius = radius;
        _tickInterval = tickInterval;
        _remainingDuration = duration;
        _moveSpeedMultiplier = moveSpeedMultiplier;
        _sourceId = GetInstanceID();
        _tickTimer = 0f;
        _rangeCheckTimer = 0f;
        _isInitialized = true;

        if (isActiveAndEnabled)
            RefreshSlowModifiers();
    }

    private void OnEnable()
    {
        if (!_isInitialized)
            return;

        _rangeCheckTimer = 0f;
        RefreshSlowModifiers();
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        float elapsed = Mathf.Min(Time.deltaTime, _remainingDuration);
        _remainingDuration = Mathf.Max(0f, _remainingDuration - elapsed);
        _rangeCheckTimer += elapsed;
        _tickTimer += elapsed;

        if (_rangeCheckTimer >= _rangeCheckInterval)
        {
            _rangeCheckTimer %= _rangeCheckInterval;
            RefreshSlowModifiers();
        }

        while (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyTickDamage();
        }

        if (_remainingDuration <= 0f)
        {
            _isInitialized = false;
            ClearSlowModifiers();
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        ClearSlowModifiers();
    }

    private void OnDestroy()
    {
        ClearSlowModifiers();
    }

    private void RefreshSlowModifiers()
    {
        HashSet<EnemyStatusEffectController> currentStatusEffects = new();

        foreach (EnemyHealth enemy in FindDamageableEnemies())
        {
            if (!enemy.TryGetComponent(out EnemyStatusEffectController statusEffects))
                continue;
            currentStatusEffects.Add(statusEffects);

            if (!_affectedStatusEffects.Contains(statusEffects))
                statusEffects.AddMoveSpeedModifier(_sourceId, _moveSpeedMultiplier);
        }

        IEnumerable<EnemyStatusEffectController> exitedStatusEffects = _affectedStatusEffects.Where(statusEffects =>
            statusEffects != null && !currentStatusEffects.Contains(statusEffects));
        foreach (EnemyStatusEffectController statusEffects in exitedStatusEffects)
        {
            statusEffects.RemoveMoveSpeedModifier(_sourceId);
        }

        _affectedStatusEffects.Clear();
        _affectedStatusEffects.UnionWith(currentStatusEffects);
    }

    private void ApplyTickDamage()
    {
        IEnumerable<EnemyHealth> damageableEnemies = FindDamageableEnemies()
            .Where(enemy => enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled);
        foreach (EnemyHealth enemy in damageableEnemies)
        {
            enemy.TakeDamage(_tickDamage);
        }
    }

    private void ClearSlowModifiers()
    {
        foreach (EnemyStatusEffectController statusEffects in _affectedStatusEffects.Where(statusEffects =>
                     statusEffects != null))
        {
            statusEffects.RemoveMoveSpeedModifier(_sourceId);
        }

        _affectedStatusEffects.Clear();
    }

    private HashSet<EnemyHealth> FindDamageableEnemies()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _radius);
        HashSet<EnemyHealth> enemies = new();

        foreach (Collider2D collider in colliders)
        {
            if (!collider.TryGetComponent(out EnemyHealth enemy))
                continue;
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled)
                continue;
            enemies.Add(enemy);
        }

        return enemies;
    }
}