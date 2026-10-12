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
    private readonly Action<Vector2, Vector2> _playHitEffect;
    private readonly Action<EnemyHealth, Vector2, EnemyHealth> _playChainLink;
    private readonly List<Collider2D> _chainCandidates = new(16);
    public BulletHitEffects(PlayerAttackData attackData, BulletRuntime runtime,
        Action<ReactionElement, ReactionElement, ReactionContext> executeReaction,
        float chainRadius, LayerMask chainLayers, Action<Vector2, Vector2> playHitEffect = null,
        Action<EnemyHealth, Vector2, EnemyHealth> playChainLink = null)
    {
        _attackData = attackData;
        _runtime = runtime;
        _executeReaction = executeReaction;
        _chainRadius = chainRadius;
        _chainEnemyLayers = chainLayers;
        _playHitEffect = playHitEffect;
        _playChainLink = playChainLink;
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

        if (enemyHealth == null)
            return;

        // 이번 적중으로 죽은 적에게는 효과를 적용하지 않고 연출만 그 자리에 재생한다.
        if (enemyHealth.IsDead || !enemyHealth.isActiveAndEnabled)
        {
            if (enemyHealth.TryGetComponent(out EnemyStatusEffectController deadStatusEffects))
                deadStatusEffects.PlayElementVisualDetached(configuration.PrimaryElement.Value, direction);
            return;
        }

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
        _playHitEffect?.Invoke(target.transform.position, direction);
        // 어느 적에게 연쇄됐는지 보이도록 직접 맞은 적과 받은 적을 잇는다. (ChainLinkFx)
        _playChainLink?.Invoke(directTarget, hitPosition, target);
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
