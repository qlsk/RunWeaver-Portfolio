using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private const float OwnerArrivalDistance = 0.1f;
    private const float ReverseSteeringDelay = 0.1f;
    private const float OrbitDuration = 5f;

    private enum BulletEndReason
    {
        Hit,
        MaxRange,
        OwnerReached,
        OwnerLost,
        OrbitTimeout
    }

    [FoldoutGroup("References"), SerializeField]
    private OverHeatReaction _overHeatReaction;

    [FoldoutGroup("References"), SerializeField]
    private SteamReaction _steamReaction;

    [FoldoutGroup("References"), SerializeField]
    private MoltenReaction _moltenReaction;

    [FoldoutGroup("References"), SerializeField]
    private FirestormReaction _firestormReaction;

    [Header("Cast Effect")] [SerializeField]
    private ParticleSystem _castEffectPrefab;

    [SerializeField, Min(0.01f)] private float _castEffectScale = 1f;
    [SerializeField, Min(0.1f)] private float _castEffectLifetime = 3f;

    [Header("Seek")] [SerializeField, Min(0f)]
    private float _seekRadius = 5f;

    [SerializeField] private LayerMask _seekEnemyLayers = ~0;
    private readonly List<Collider2D> _seekCandidates = new(16);

    [Header("Fix")] [SerializeField] private FixTrap _fixTrapPrefab;

    [Header("Chain")] [SerializeField, Min(0f)]
    private float _chainRadius = 5f;

    [SerializeField] private LayerMask _chainEnemyLayers = ~0;
    private readonly List<Collider2D> _chainCandidates = new(16);

    // Attack data
    private PlayerAttackData _attackData;

    // Runtime state
    private BulletRuntime _runtime;
    private bool _isInitialized;
    private bool _processingHit;

    /// 공격 데이터를 저장하고 이동 거리와 적중 처리 상태를 초기화한다.
    public void Initialize(PlayerAttackData attackData)
    {
        _attackData = attackData.WithDirection(attackData.Direction.normalized);

        _runtime = new BulletRuntime
        {
            Phase = BulletPhase.Flying,
            Direction = _attackData.Direction,
            ReverseUsed = false
        };
        _processingHit = false;
        _isInitialized = true;
        UpdateRotation();
    }

    /// 발사 위치에 시각 효과를 재생하고 설정된 시간이 지나면 제거한다.
    public void PlayCastEffect(Transform muzzle)
    {
        if (_castEffectPrefab == null || muzzle == null) return;

        ParticleSystem effect = Instantiate(_castEffectPrefab, muzzle, false);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        effect.transform.localScale *= _castEffectScale;

        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.stopAction = ParticleSystemStopAction.None;
            var trails = particles.trails;
            trails.worldSpace = false;
        }

        effect.gameObject.SetActive(true);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
        Destroy(effect.gameObject, _castEffectLifetime);
    }

    /// 비행 상태에 따라 일반 이동, 플레이어 복귀 또는 궤도 회전을 처리한다.
    private void Update()
    {
        if (!_isInitialized)
            return;

        if (_attackData.Speed <= 0f && _runtime.Phase != BulletPhase.Orbiting)
        {
            DestroyBullet();
            return;
        }

        if (_runtime.Phase == BulletPhase.Returning)
            MoveReturning();
        else if (_runtime.Phase == BulletPhase.Flying)
            MoveFlying();
        else if (_runtime.Phase == BulletPhase.Orbiting)
            MoveOrbiting();
    }

    private void MoveFlying()
    {
        float deltaTime = Time.deltaTime;
        float reverseTime = Mathf.Min(deltaTime, _runtime.ReverseSteeringRemaining);
        if (reverseTime > 0f)
        {
            MoveFlyingInDirection(reverseTime);
            _runtime.ReverseSteeringRemaining -= reverseTime;
            deltaTime -= reverseTime;
        }

        if (deltaTime > 0f && _runtime.TravelDistance < _attackData.Range)
        {
            if (_attackData.Configuration.IsSeek)
                UpdateSeekDirection();
            MoveFlyingInDirection(deltaTime);
        }

        if (_runtime.TravelDistance >= _attackData.Range)
            RequestEnd(BulletEndReason.MaxRange);
    }

    private void MoveFlyingInDirection(float deltaTime)
    {
        float remainingDistance = Mathf.Max(0f, _attackData.Range - _runtime.TravelDistance);
        float moveDistance = Mathf.Min(_attackData.Speed * deltaTime, remainingDistance);
        transform.position += (Vector3)(_runtime.Direction * moveDistance);
        _runtime.TravelDistance += moveDistance;
        _runtime.TotalTravelDistance += moveDistance;
    }

    /// 일반 비행 중 가장 가까운 미적중 적을 추적하며, 없으면 현재 방향을 유지한다.
    private void UpdateSeekDirection()
    {
        if (_seekRadius <= 0f)
            return;

        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_seekEnemyLayers);
        _seekCandidates.Clear();
        // 재사용 List는 필요 시 확장되어 고정 버퍼로 인한 대상 누락을 방지한다.
        Physics2D.OverlapCircle(transform.position, _seekRadius, filter, _seekCandidates);

        EnemyHealth closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in _seekCandidates)
        {
            EnemyHealth enemy = candidate.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled || _runtime.HasHitTarget(enemy))
                continue;

            float distance = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closest = enemy;
            closestDistance = distance;
        }

        if (closest == null)
            return;

        Vector2 offset = closest.transform.position - transform.position;
        if (offset.sqrMagnitude <= 0f)
            return;

        _runtime.Direction = offset.normalized;
        UpdateRotation();
    }

    private void MoveReturning()
    {
        Transform owner = _attackData.Owner;
        if (owner == null)
        {
            RequestEnd(BulletEndReason.OwnerLost);
            return;
        }

        Vector2 offset = owner.position - transform.position;
        if (offset.sqrMagnitude <= OwnerArrivalDistance * OwnerArrivalDistance)
        {
            RequestEnd(BulletEndReason.OwnerReached);
            return;
        }

        float deltaTime = Time.deltaTime;
        float reverseTime = Mathf.Min(deltaTime, _runtime.ReverseSteeringRemaining);
        if (reverseTime > 0f)
        {
            transform.position += (Vector3)(_runtime.Direction * (_attackData.Speed * reverseTime));
            _runtime.TotalTravelDistance += _attackData.Speed * reverseTime;
            _runtime.ReverseSteeringRemaining -= reverseTime;
            deltaTime -= reverseTime;
        }

        if (deltaTime > 0f)
        {
            offset = owner.position - transform.position;
            _runtime.Direction = offset.normalized;
            float distance = Mathf.Min(_attackData.Speed * deltaTime, offset.magnitude);
            transform.position += (Vector3)(_runtime.Direction * distance);
            _runtime.TotalTravelDistance += distance;
            UpdateRotation();
        }

        if (((Vector2)(owner.position - transform.position)).sqrMagnitude <=
            OwnerArrivalDistance * OwnerArrivalDistance)
            RequestEnd(BulletEndReason.OwnerReached);
    }

    private void EnterOrbiting()
    {
        Vector2 offset = transform.position - _attackData.Owner.position;
        _runtime.Phase = BulletPhase.Orbiting;
        _runtime.OrbitElapsed = 0f;
        _runtime.OrbitRadius = Mathf.Max(offset.magnitude, 0.01f);
        _runtime.OrbitAngle = Mathf.Atan2(offset.y, offset.x);
        _runtime.ReverseSteeringRemaining = 0f;
        UpdateOrbitDirection();
    }

    /// 플레이어의 현재 위치를 중심으로 탄환 속도를 접선 속도로 사용해 5초간 회전한다.
    private void MoveOrbiting()
    {
        if (_attackData.Owner == null)
        {
            RequestEnd(BulletEndReason.OwnerLost);
            return;
        }

        float deltaTime = Mathf.Min(Time.deltaTime, Mathf.Max(0f, OrbitDuration - _runtime.OrbitElapsed));
        float distance = Mathf.Max(0f, _attackData.Speed) * deltaTime;
        _runtime.OrbitAngle += distance / _runtime.OrbitRadius * _runtime.OrbitDirection;
        Vector2 radial = new Vector2(Mathf.Cos(_runtime.OrbitAngle), Mathf.Sin(_runtime.OrbitAngle));
        Vector2 nextPosition = (Vector2)_attackData.Owner.position + radial * _runtime.OrbitRadius;
        transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
        _runtime.TotalTravelDistance += distance;
        _runtime.OrbitElapsed += deltaTime;
        UpdateOrbitDirection();

        if (_runtime.OrbitElapsed >= OrbitDuration)
            RequestEnd(BulletEndReason.OrbitTimeout);
    }

    private void UpdateOrbitDirection()
    {
        _runtime.Direction = new Vector2(-Mathf.Sin(_runtime.OrbitAngle), Mathf.Cos(_runtime.OrbitAngle)) *
                             _runtime.OrbitDirection;
        UpdateRotation();
    }

    /// Orbit에서는 Pierce가 없으면 최초 Reverse가 발동해도 적중 후 종료한다.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isInitialized || _processingHit)
            return;

        EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null || enemyHealth.IsDead || !enemyHealth.isActiveAndEnabled)
            return;

        if (!_runtime.TryRegisterTarget(enemyHealth))
            return;

        bool isPierce = _attackData.Configuration.IsPierce;
        float hitDamage = _attackData.Damage * _runtime.GetDamageMultiplier(isPierce);
        _runtime.RecordHit();
        Vector2 hitPosition = collision.ClosestPoint(transform.position);
        bool reversedThisHit = false;
        _processingHit = true;
        try
        {
            enemyHealth.TakeDamage(hitDamage);
            ApplyElementEffect(enemyHealth, hitPosition, hitDamage);
            ApplyChain(enemyHealth, hitPosition, hitDamage);
            reversedThisHit = ApplyReverse();
        }
        finally
        {
            _processingHit = false;
            if (!isPierce && (_runtime.Phase == BulletPhase.Orbiting || !reversedThisHit))
                RequestEnd(BulletEndReason.Hit);
        }
    }

    /// 탄환의 전체 생명주기에서 한 번만 반전하고 이번 적중의 발동 여부를 반환한다.
    private bool ApplyReverse()
    {
        if (!_isInitialized || !_attackData.Configuration.IsReverse || _runtime.ReverseUsed)
            return false;

        _runtime.ReverseUsed = true;
        if (_runtime.Phase == BulletPhase.Orbiting)
        {
            _runtime.OrbitDirection *= -1f;
            UpdateOrbitDirection();
            return true;
        }

        _runtime.Direction = -_runtime.Direction;
        _runtime.ReverseSteeringRemaining = ReverseSteeringDelay;
        UpdateRotation();
        return true;
    }

    private void UpdateRotation()
    {
        float angle = Mathf.Atan2(_runtime.Direction.y, _runtime.Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// 두 원소의 반응을 실행하거나 살아 있는 적에게 단일 원소 효과를 적용한다.
    private void ApplyElementEffect(EnemyHealth enemyHealth, Vector2 hitPosition, float hitDamage)
    {
        RuneEffectConfiguration configuration = _attackData.Configuration;
        if (!configuration.PrimaryElement.HasValue)
            return;

        if (configuration.SecondaryElement.HasValue)
        {
            switch (configuration.PrimaryElement.Value, configuration.SecondaryElement.Value)
            {
                case (ReactionElement.Fire, ReactionElement.Fire):
                    _overHeatReaction.Execute(hitPosition, hitDamage);
                    break;
                case (ReactionElement.Fire, ReactionElement.Water):
                case (ReactionElement.Water, ReactionElement.Fire):
                    _steamReaction.Execute(hitPosition, hitDamage);
                    break;
                case (ReactionElement.Fire, ReactionElement.Earth):
                case (ReactionElement.Earth, ReactionElement.Fire):
                    _moltenReaction.Execute(hitPosition, hitDamage);
                    break;
                case (ReactionElement.Fire, ReactionElement.Wind):
                case (ReactionElement.Wind, ReactionElement.Fire):
                    _firestormReaction.Execute(hitPosition, hitDamage);
                    break;
            }

            return;
        }

        if (enemyHealth == null || enemyHealth.IsDead || !enemyHealth.isActiveAndEnabled)
            return;

        if (!enemyHealth.TryGetComponent(out EnemyStatusEffectController statusEffects))
            throw new InvalidOperationException($"EnemyStatusEffectController is missing: {enemyHealth.name}");

        statusEffects.ApplyElementEffect(configuration.PrimaryElement.Value, hitDamage, _runtime.Direction);
    }

    /// 직접 적중마다 다른 적 한 명에게 주 원소 효과를 적용한다. 반응 피해에서는 호출하지 않는다.
    private void ApplyChain(EnemyHealth directTarget, Vector2 hitPosition, float hitDamage)
    {
        RuneEffectConfiguration configuration = _attackData.Configuration;
        if (!configuration.IsChain || !configuration.PrimaryElement.HasValue || _chainRadius <= 0f)
            return;

        EnemyStatusEffectController target = FindChainTarget(directTarget, hitPosition);
        if (target == null)
            return;

        Vector2 direction = (Vector2)target.transform.position - hitPosition;
        direction = direction.sqrMagnitude > 0f ? direction.normalized : _runtime.Direction;
        target.ApplyElementEffect(configuration.PrimaryElement.Value, hitDamage, direction);
    }

    private EnemyStatusEffectController FindChainTarget(EnemyHealth directTarget, Vector2 hitPosition)
    {
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_chainEnemyLayers);
        _chainCandidates.Clear();
        Physics2D.OverlapCircle(hitPosition, _chainRadius, filter, _chainCandidates);

        EnemyStatusEffectController closest = null;
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

            closest = statusEffects;
            closestDistance = distance;
        }

        return closest;
    }

    /// 일반 비행 종료에 Return, Orbit 또는 Fix가 개입한다.
    private void RequestEnd(BulletEndReason reason)
    {
        if (!_isInitialized)
            return;

        if (_runtime.Phase == BulletPhase.Flying && reason == BulletEndReason.MaxRange &&
            _attackData.Configuration.IsFix)
        {
            Vector3 position = transform.position;
            // 먼저 종료 상태를 확정해 같은 프레임의 중복 생성과 추가 적중을 차단한다.
            DestroyBullet();
            if (_fixTrapPrefab == null)
                throw new InvalidOperationException("FixTrap prefab is missing.");

            Instantiate(_fixTrapPrefab, position, Quaternion.identity);
            return;
        }

        if (_runtime.Phase == BulletPhase.Flying && reason == BulletEndReason.MaxRange &&
            _attackData.Configuration.IsOrbit && _attackData.Owner != null)
        {
            EnterOrbiting();
            return;
        }

        if (_runtime.Phase == BulletPhase.Flying && _attackData.Configuration.IsReturn &&
            !_runtime.ReturnUsed && _attackData.Owner != null &&
            (reason == BulletEndReason.Hit || reason == BulletEndReason.MaxRange))
        {
            _runtime.ReturnUsed = true;
            _runtime.Phase = BulletPhase.Returning;
            _runtime.ReverseSteeringRemaining = 0f;
            _runtime.Direction = ((Vector2)(_attackData.Owner.position - transform.position)).normalized;
            UpdateRotation();
            return;
        }

        DestroyBullet();
    }

    /// 이동과 적중 처리를 중단하고 탄환 오브젝트를 제거한다.
    private void DestroyBullet()
    {
        if (_runtime != null)
            _runtime.Phase = BulletPhase.Ended;
        _isInitialized = false;
        Destroy(gameObject);
    }
}
