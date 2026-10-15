using System;
using System.Collections.Generic;
using UnityEngine;

// 원소 반응과 연쇄 효과를 담당한다.
public sealed class BulletHitEffects
{
    private readonly PlayerAttackData _attackData;
    private readonly BulletRuntime _runtime;
    private readonly Action<ReactionElement, ReactionElement, ReactionContext> _executeReaction;
    private readonly float _chainRadius;
    private readonly LayerMask _chainEnemyLayers;
    private readonly List<Collider2D> _chainCandidates = new(16);
    public BulletHitEffects(PlayerAttackData attackData, BulletRuntime runtime,
        Action<ReactionElement, ReactionElement, ReactionContext> executeReaction,
        float chainRadius, LayerMask chainLayers)
    {
        _attackData = attackData;
        _runtime = runtime;
        _executeReaction = executeReaction;
        _chainRadius = chainRadius;
        _chainEnemyLayers = chainLayers;
    }
    /// 두 원소의 반응을 실행하거나 살아 있는 적에게 단일 원소 효과를 적용한다.
    public void ApplyElementEffect(EnemyHealth enemyHealth, Vector2 hitPosition, float hitDamage)
    {
        ApplyElementEffect(enemyHealth, hitPosition, hitDamage, _runtime.Direction);
    }

    private void ApplyElementEffect(EnemyHealth enemyHealth, Vector2 hitPosition, float hitDamage,
        Vector2 direction)
    {
        RuneEffectConfiguration configuration = _attackData.Configuration;
        if (!configuration.PrimaryElement.HasValue)
            return;

        if (configuration.SecondaryElement.HasValue)
        {
            _executeReaction(configuration.PrimaryElement.Value, configuration.SecondaryElement.Value,
                new ReactionContext(hitPosition, hitDamage, direction));
            return;
        }

        if (enemyHealth == null || enemyHealth.IsDead || !enemyHealth.isActiveAndEnabled)
            return;

        if (!enemyHealth.TryGetComponent(out EnemyStatusEffectController statusEffects))
            throw new InvalidOperationException($"EnemyStatusEffectController is missing: {enemyHealth.name}");

        statusEffects.ApplyElementEffect(configuration.PrimaryElement.Value, hitDamage, direction);
    }

    /// 직접 적중마다 다른 적 한 명에게 단일 원소 또는 두 원소 반응을 적용한다.
    /// 원소 처리 함수는 Chain을 호출하지 않으므로 반응에서 연쇄가 재귀적으로 발생하지 않는다.
    public void ApplyChain(EnemyHealth directTarget, Vector2 hitPosition, float hitDamage)
    {
        RuneEffectConfiguration configuration = _attackData.Configuration;
        if (!configuration.IsChain || !configuration.PrimaryElement.HasValue || _chainRadius <= 0f)
            return;

        EnemyHealth target = FindChainTarget(directTarget, hitPosition);
        if (target == null)
            return;

        Vector2 direction = (Vector2)target.transform.position - hitPosition;
        direction = direction.sqrMagnitude > 0f ? direction.normalized : _runtime.Direction;
        ApplyElementEffect(target, target.transform.position, hitDamage, direction);
    }

    private EnemyHealth FindChainTarget(EnemyHealth directTarget, Vector2 hitPosition)
    {
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_chainEnemyLayers);
        _chainCandidates.Clear();
        Physics2D.OverlapCircle(hitPosition, _chainRadius, filter, _chainCandidates);

        EnemyHealth closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in _chainCandidates)
        {
            EnemyHealth enemy = candidate.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy == directTarget || enemy.IsDead || !enemy.isActiveAndEnabled)
                continue;

            if (!enemy.TryGetComponent(out EnemyStatusEffectController statusEffects) ||
                !statusEffects.isActiveAndEnabled)
                continue;

            float distance = ((Vector2)enemy.transform.position - hitPosition).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closest = enemy;
            closestDistance = distance;
        }

        return closest;
    }

}
