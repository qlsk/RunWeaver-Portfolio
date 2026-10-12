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
    private readonly List<EnemyHealth> _chainTargets = new(4);
    private readonly List<float> _chainDistances = new(4);
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

    /// 직접 적중마다 가까운 다른 적들에게 단일 원소 또는 두 원소 반응을 적용한다.
    /// 연쇄하는 적의 수는 Chain 장착 개수와 같다. (1개면 1마리, 2개면 2마리 ...)
    /// 모두 직접 맞은 자리에서 뻗어 나가며, 원소 처리 함수는 Chain을 호출하지 않으므로 반응에서 연쇄가 재귀적으로 발생하지 않는다.
    public void ApplyChain(EnemyHealth directTarget, Vector2 hitPosition, float hitDamage)
    {
        RuneEffectConfiguration configuration = _attackData.Configuration;
        if (!configuration.IsChain || !configuration.PrimaryElement.HasValue || _chainRadius <= 0f)
            return;

        FindChainTargets(directTarget, hitPosition, configuration.ChainCount);
        foreach (EnemyHealth target in _chainTargets)
        {
            Vector2 direction = (Vector2)target.transform.position - hitPosition;
            direction = direction.sqrMagnitude > 0f ? direction.normalized : _runtime.Direction;
            _playHitEffect?.Invoke(target.transform.position, direction);
            // 어느 적에게 연쇄됐는지 보이도록 직접 맞은 적과 받은 적을 잇는다. (ChainLinkFx)
            _playChainLink?.Invoke(directTarget, hitPosition, target);
            ApplyElementEffect(target, target.transform.position, hitDamage, direction);
        }

        _chainTargets.Clear();
    }

    /// 반경 안에서 직접 맞은 적을 뺀 가장 가까운 적을 count마리까지 가까운 순서로 _chainTargets에 담는다.
    private void FindChainTargets(EnemyHealth directTarget, Vector2 hitPosition, int count)
    {
        _chainTargets.Clear();
        _chainDistances.Clear();
        if (count <= 0)
            return;

        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_chainEnemyLayers);
        _chainCandidates.Clear();
        Physics2D.OverlapCircle(hitPosition, _chainRadius, filter, _chainCandidates);

        foreach (Collider2D candidate in _chainCandidates)
        {
            EnemyHealth enemy = candidate.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy == directTarget || enemy.IsDead || !enemy.isActiveAndEnabled)
                continue;

            // 콜라이더가 여러 개인 적은 한 번만 센다.
            if (_chainTargets.Contains(enemy))
                continue;

            if (!enemy.TryGetComponent(out EnemyStatusEffectController statusEffects) ||
                !statusEffects.isActiveAndEnabled)
                continue;

            float distance = ((Vector2)enemy.transform.position - hitPosition).sqrMagnitude;
            if (_chainTargets.Count >= count && distance >= _chainDistances[_chainTargets.Count - 1])
                continue;

            // 가까운 순서를 유지하며 끼워 넣고, count마리를 넘으면 가장 먼 적을 뺀다.
            int index = _chainDistances.Count;
            while (index > 0 && _chainDistances[index - 1] > distance)
                index--;

            _chainTargets.Insert(index, enemy);
            _chainDistances.Insert(index, distance);
            if (_chainTargets.Count > count)
            {
                _chainTargets.RemoveAt(_chainTargets.Count - 1);
                _chainDistances.RemoveAt(_chainDistances.Count - 1);
            }
        }
    }

}
