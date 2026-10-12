using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
    private readonly float _chainDelay;
    public BulletHitEffects(PlayerAttackData attackData, BulletRuntime runtime,
        Action<ReactionElement, ReactionElement, ReactionContext> executeReaction,
        float chainRadius, LayerMask chainLayers, Action<Vector2, Vector2> playHitEffect = null,
        Action<EnemyHealth, Vector2, EnemyHealth> playChainLink = null, float chainDelay = 0.1f)
    {
        _attackData = attackData;
        _runtime = runtime;
        _executeReaction = executeReaction;
        _chainRadius = chainRadius;
        _chainEnemyLayers = chainLayers;
        _playHitEffect = playHitEffect;
        _playChainLink = playChainLink;
        _chainDelay = Mathf.Max(0f, chainDelay);
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

    /// 직접 적중마다 연쇄를 일으킨다. 단일 원소 또는 두 원소 반응을 적용한다.
    /// - Chain 장착 개수만큼 이어진다. (1개면 1번, 2개면 2번 ...)
    /// - 직접 맞은 적에서 가장 가까운 적으로 튀고, 거기서 다시 가장 가까운 적으로 튄다. (반경은 매번 연쇄한 적 기준)
    /// - 한 번의 연쇄 안에서 이미 맞았거나 연쇄됐던 적은 다시 대상이 되지 않는다.
    /// - 한 번 튈 때마다 _chainDelay(기본 0.1초, 게임 시간)만큼 늦게 적용한다.
    /// - 연쇄 피해는 매번 직접 맞은 피해의 50%다. (RuneEffectConfiguration.ChainDamageMultiplier)
    /// 원소 처리 함수는 Chain을 호출하지 않으므로 반응에서 연쇄가 재귀적으로 발생하지 않는다.
    /// 탄환이 먼저 사라져도 연쇄는 끝까지 이어진다.
    public void ApplyChain(EnemyHealth directTarget, Vector2 hitPosition, float hitDamage)
    {
        RuneEffectConfiguration configuration = _attackData.Configuration;
        if (!configuration.IsChain || !configuration.PrimaryElement.HasValue || _chainRadius <= 0f)
            return;

        float chainDamage = hitDamage * configuration.ChainDamageMultiplier;
        RunChainAsync(directTarget, hitPosition, chainDamage, configuration.ChainCount).Forget();
    }

    private async UniTaskVoid RunChainAsync(EnemyHealth directTarget, Vector2 hitPosition, float chainDamage,
        int count)
    {
        var visited = new HashSet<EnemyHealth>();
        if (directTarget != null)
            visited.Add(directTarget);

        EnemyHealth from = directTarget;
        Vector2 fromPosition = hitPosition;
        for (int i = 0; i < count; i++)
        {
            if (_chainDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(_chainDelay), DelayType.DeltaTime);

            // 앞 대상이 아직 있으면 지금 위치에서 찾는다. (사라졌으면 마지막 위치)
            if (from != null)
                fromPosition = from.transform.position;

            EnemyHealth target = FindChainTarget(fromPosition, visited);
            if (target == null)
                return;

            visited.Add(target);
            Vector2 targetPosition = target.transform.position;
            Vector2 direction = targetPosition - fromPosition;
            direction = direction.sqrMagnitude > 0f ? direction.normalized : _runtime.Direction;
            _playHitEffect?.Invoke(targetPosition, direction);
            // 어느 적에게 연쇄됐는지 보이도록 앞 대상과 이번 대상을 잇는다. (ChainLinkFx)
            _playChainLink?.Invoke(from, fromPosition, target);
            ApplyElementEffect(target, targetPosition, chainDamage, direction);

            from = target;
            fromPosition = targetPosition;
        }
    }

    /// center에서 반경 안의 가장 가까운 적을 찾는다. visited에 있는 적은 뺀다.
    private EnemyHealth FindChainTarget(Vector2 center, HashSet<EnemyHealth> visited)
    {
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_chainEnemyLayers);
        _chainCandidates.Clear();
        Physics2D.OverlapCircle(center, _chainRadius, filter, _chainCandidates);

        EnemyHealth closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in _chainCandidates)
        {
            if (candidate == null)
                continue;

            EnemyHealth enemy = candidate.GetComponentInParent<EnemyHealth>();
            if (enemy == null || visited.Contains(enemy) || enemy.IsDead || !enemy.isActiveAndEnabled)
                continue;

            if (!enemy.TryGetComponent(out EnemyStatusEffectController statusEffects) ||
                !statusEffects.isActiveAndEnabled)
                continue;

            float distance = ((Vector2)enemy.transform.position - center).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closest = enemy;
            closestDistance = distance;
        }

        _chainCandidates.Clear();
        return closest;
    }

}
