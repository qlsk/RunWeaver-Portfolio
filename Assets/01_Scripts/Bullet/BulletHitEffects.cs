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
    private readonly List<Collider2D> _chainCandidates = new(16);
    private List<EchoReservation> _echoReservations;

    public bool HasPendingEcho => _echoReservations != null && _echoReservations.Count > 0;

    public BulletHitEffects(PlayerAttackData attackData, BulletRuntime runtime,
        Action<ReactionElement, ReactionElement, ReactionContext> executeReaction,
        float chainRadius, LayerMask chainLayers, Action<Vector2, Vector2> playHitEffect = null)
    {
        _attackData = attackData;
        _runtime = runtime;
        _executeReaction = executeReaction;
        _chainRadius = chainRadius;
        _chainEnemyLayers = chainLayers;
        _playHitEffect = playHitEffect;
    }

    /// 두 원소의 반응을 실행하거나 살아 있는 적에게 단일 원소 효과를 적용한다.
    public void ApplyElementEffect(EnemyHealth enemyHealth, Vector2 hitPosition, float hitDamage)
    {
        ApplyElementEffect(enemyHealth, hitPosition, hitDamage, _runtime.Direction);
    }

    private void ApplyElementEffect(EnemyHealth enemyHealth, Vector2 hitPosition, float hitDamage,
        Vector2 direction)
    {
        if (ApplyElementEffectCore(enemyHealth, hitPosition, hitDamage, direction))
            ReserveEcho(enemyHealth, hitPosition, hitDamage, direction);
    }

    /// 원소 효과나 반응을 예약 없이 적용한다. 효과가 실제로 적용되면 true를 반환한다.
    private bool ApplyElementEffectCore(EnemyHealth enemyHealth, Vector2 hitPosition, float hitDamage,
        Vector2 direction)
    {
        RuneEffectConfiguration configuration = _attackData.Configuration;
        if (!configuration.PrimaryElement.HasValue)
            return false;

        if (configuration.SecondaryElement.HasValue)
        {
            _executeReaction(configuration.PrimaryElement.Value, configuration.SecondaryElement.Value,
                new ReactionContext(hitPosition, hitDamage, direction));
            return true;
        }

        if (enemyHealth == null)
            return false;

        // 이번 적중으로 죽은 적에게는 효과를 적용하지 않고 연출만 그 자리에 재생한다.
        if (enemyHealth.IsDead || !enemyHealth.isActiveAndEnabled)
        {
            if (enemyHealth.TryGetComponent(out EnemyStatusEffectController deadStatusEffects))
                deadStatusEffects.PlayElementVisualDetached(configuration.PrimaryElement.Value, direction);
            return false;
        }

        if (!enemyHealth.TryGetComponent(out EnemyStatusEffectController statusEffects))
            throw new InvalidOperationException($"EnemyStatusEffectController is missing: {enemyHealth.name}");

        statusEffects.ApplyElementEffect(configuration.PrimaryElement.Value, hitDamage, direction);
        return true;
    }

    private void ReserveEcho(EnemyHealth enemyHealth, Vector2 hitPosition, float hitDamage, Vector2 direction)
    {
        if (_attackData.Configuration.EchoCount == 0)
            return;

        _echoReservations ??= new List<EchoReservation>();

        for (int i = 1; i <= _attackData.Configuration.EchoCount; i++)
        {
            float remainingSeconds = i * RuneEffectConfiguration.EchoDelaySeconds;
            ReactionContext context = new(hitPosition, hitDamage, direction);
            EchoReservation reservation = new(remainingSeconds, enemyHealth, context);
            _echoReservations.Add(reservation);
        }
    }

    public void TickEcho(float deltaTime)
    {
        if (_echoReservations == null)
            return;

        for (int i = _echoReservations.Count - 1; i >= 0; i--)
        {
            EchoReservation echoReservation = _echoReservations[i];
            echoReservation.RemainingSeconds -= deltaTime;
            _echoReservations[i] = echoReservation;

            if (echoReservation.RemainingSeconds > 0)
                continue;

            // 실행 중 예외가 나도 같은 예약이 매 프레임 재실행되지 않도록 먼저 제거한다.
            _echoReservations.RemoveAt(i);

            if (!CanExecuteEcho(echoReservation))
                continue;

            ApplyElementEffectCore(echoReservation.Target,
                echoReservation.Context.HitPosition,
                echoReservation.Context.BulletDamage,
                echoReservation.Context.BulletDirection);
        }
    }

    /// 단일 원소 반복은 대상이 사라졌거나 죽었으면 사망 연출 없이 건너뛴다. 2원소 반응은 위치 기준이라 항상 실행한다.
    private bool CanExecuteEcho(EchoReservation reservation)
    {
        if (_attackData.Configuration.SecondaryElement.HasValue)
            return true;

        return reservation.Target != null && !reservation.Target.IsDead && reservation.Target.isActiveAndEnabled;
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