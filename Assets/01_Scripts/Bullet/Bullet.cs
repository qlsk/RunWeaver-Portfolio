using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private const float OwnerArrivalDistance = 0.1f;
    private const float ReverseSteeringDelay = 0.1f;
    private const float OrbitDuration = 5f;

    [FoldoutGroup("References"), SerializeField] private OverHeatReaction _overHeatReaction;
    [FoldoutGroup("References"), SerializeField] private SteamReaction _steamReaction;
    [FoldoutGroup("References"), SerializeField] private MoltenReaction _moltenReaction;
    [FoldoutGroup("References"), SerializeField] private FirestormReaction _firestormReaction;
    [FoldoutGroup("References"), SerializeField] private DelugeReaction _delugeReaction;
    [FoldoutGroup("References"), SerializeField] private MireReaction _mireReaction;
    [FoldoutGroup("References"), SerializeField] private TempestReaction _tempestReaction;
    [FoldoutGroup("References"), SerializeField] private PetrifyReaction _petrifyReaction;
    [FoldoutGroup("References"), SerializeField] private CycloneReaction _cycloneReaction;
    [FoldoutGroup("References"), SerializeField] private SandstormReaction _sandstormReaction;

    [FoldoutGroup("Movement Settings"), SerializeField, Min(0.01f)] private float _startSpeed = 20f;
    [FoldoutGroup("Movement Settings"), SerializeField, Min(0.01f)] private float _endSpeed = 10f;

    [Header("Seek")] [SerializeField, Min(0f)]
    private float _seekRadius = 5f;

    [SerializeField] private LayerMask _seekEnemyLayers = ~0;

    [Header("Fix")] [SerializeField] private FixTrap _fixTrapPrefab;

    [Header("Chain")] [SerializeField, Min(0f)]
    private float _chainRadius = 5f;

    [SerializeField] private LayerMask _chainEnemyLayers = ~0;

    [Tooltip("연쇄 대상을 잇는 연출 (ChainLinkFx 프리팹). 비워두면 연출 없이 연쇄만 한다.")]
    [SerializeField] private ChainLinkFx _chainLinkFxPrefab;

    private BulletMovement _movement;
    private BulletHitEffects _hitEffects;
    private BulletVisualEffects _visualEffects;
    private GameObject _projectileVisual;
    public event Action<BulletEndReason> Ended;

    // Attack data
    private PlayerAttackData _attackData;

    // Runtime state
    private BulletRuntime _runtime;
    private bool _isInitialized;
    private bool _processingHit;

    /// 공격 데이터를 저장하고 이동 거리와 적중 처리 상태를 초기화한다.
    public void Initialize(PlayerAttackData attackData, BulletVisualEffects visualEffects = null)
    {
        if (_movement != null) _movement.EndRequested -= RequestEnd;
        _attackData = attackData.WithDirection(attackData.Direction.normalized);

        _runtime = new BulletRuntime
        {
            Phase = BulletPhase.Flying,
            Direction = _attackData.Direction,
            ReverseUsed = false
        };
        _processingHit = false;
        _isInitialized = true;
        _movement = new BulletMovement(transform, _attackData, _runtime,
            _seekRadius, _seekEnemyLayers,
            OwnerArrivalDistance, ReverseSteeringDelay, OrbitDuration, _startSpeed, _endSpeed);
        _movement.EndRequested += RequestEnd;
        _hitEffects = new BulletHitEffects(_attackData, _runtime,
            ExecuteReaction,
            _chainRadius, _chainEnemyLayers, PlayHitEffect, PlayChainLink);
        _movement.UpdateRotation();
        InitializeVisuals(visualEffects);
    }

    /// 선택된 원소의 비행 효과를 탄환 자식으로 재생한다.
    private void InitializeVisuals(BulletVisualEffects visualEffects)
    {
        _visualEffects = visualEffects;
        if (_projectileVisual != null)
        {
            _projectileVisual.SetActive(false);
            Destroy(_projectileVisual);
            _projectileVisual = null;
        }

        if (visualEffects?.ProjectilePrefab == null)
            return;

        if (TryGetComponent(out SpriteRenderer sprite)) sprite.enabled = false;

        _projectileVisual = Instantiate(visualEffects.ProjectilePrefab, transform, false);
        _projectileVisual.transform.localPosition = Vector3.zero;
        _projectileVisual.transform.localRotation = Quaternion.Euler(0f, visualEffects.ProjectileRotationY, 0f);
        _projectileVisual.transform.localScale *= visualEffects.ProjectileScale;
        _projectileVisual.SetActive(true);
        foreach (ParticleSystem particles in _projectileVisual.GetComponentsInChildren<ParticleSystem>())
        {
            var main = particles.main;
            main.stopAction = ParticleSystemStopAction.None;
            particles.Play(false);
        }
    }

    /// 적중 효과는 탄환과 독립적으로 재생하여 탄환이 제거된 뒤에도 남는다.
    /// 연쇄 : 직접 맞은 적에서 연쇄를 받은 적까지 주력 원소 색으로 잇는다.
    private void PlayChainLink(EnemyHealth source, Vector2 hitPosition, EnemyHealth target)
    {
        if (_chainLinkFxPrefab == null || target == null || !_attackData.Configuration.PrimaryElement.HasValue)
            return;

        ChainLinkFx.Play(_chainLinkFxPrefab, source != null ? source.transform : null, hitPosition,
            target.transform, _attackData.Configuration.PrimaryElement.Value);
    }

    private void PlayHitEffect(Vector2 position, Vector2 direction)
    {
        if (_visualEffects == null) return;
        BulletCastEffect.PlayAt(position, direction, _visualEffects.HitPrefab,
            _visualEffects.HitScale, _visualEffects.HitLifetime);
    }

    /// 비행 상태에 따라 일반 이동, 플레이어 복귀 또는 궤도 회전을 처리한다.
    private void Update()
    {
        if (!_isInitialized)
            return;

        if (_attackData.Speed <= 0f && _runtime.Phase == BulletPhase.Returning)
        {
            DestroyBullet();
            return;
        }

        _movement.Tick(Time.deltaTime);
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
            PlayHitEffect(hitPosition, _runtime.Direction);
            float actualDamage = enemyHealth.ApplyDamage(hitDamage);
            if (actualDamage > 0f && _attackData.Owner != null &&
                _attackData.Owner.TryGetComponent(out PlayerAttack playerAttack))
                playerAttack.PlayHitFeedback(hitPosition, enemyHealth.IsDead);
            _hitEffects.ApplyElementEffect(enemyHealth, hitPosition, hitDamage);
            _hitEffects.ApplyChain(enemyHealth, hitPosition, hitDamage);
            reversedThisHit = _movement.ApplyReverse();
        }
        finally
        {
            _processingHit = false;
            if (!isPierce && (_runtime.Phase == BulletPhase.Orbiting || !reversedThisHit))
                RequestEnd(BulletEndReason.Hit);
        }
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
            DestroyBullet(reason);
            if (_fixTrapPrefab == null)
                throw new InvalidOperationException("FixTrap prefab is missing.");

            Instantiate(_fixTrapPrefab, position, Quaternion.identity);
            return;
        }

        if (_runtime.Phase == BulletPhase.Flying && reason == BulletEndReason.MaxRange &&
            _attackData.Configuration.IsOrbit && _attackData.Owner != null)
        {
            _movement.EnterOrbiting();
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
            _movement.UpdateRotation();
            return;
        }

        DestroyBullet(reason);
    }

    /// 원소 조합에 해당하는 반응을 실행하며 서로 다른 원소의 순서는 구분하지 않는다.
    private void ExecuteReaction(ReactionElement primary, ReactionElement secondary, ReactionContext context)
    {
        switch (primary, secondary)
        {
            case (ReactionElement.Fire, ReactionElement.Fire):
                _overHeatReaction.Execute(context);
                break;
            case (ReactionElement.Fire, ReactionElement.Water):
            case (ReactionElement.Water, ReactionElement.Fire):
                _steamReaction.Execute(context);
                break;
            case (ReactionElement.Fire, ReactionElement.Earth):
            case (ReactionElement.Earth, ReactionElement.Fire):
                _moltenReaction.Execute(context);
                break;
            case (ReactionElement.Fire, ReactionElement.Wind):
            case (ReactionElement.Wind, ReactionElement.Fire):
                _firestormReaction.Execute(context);
                break;
            case (ReactionElement.Water, ReactionElement.Water):
                _delugeReaction.Execute(context);
                break;
            case (ReactionElement.Water, ReactionElement.Earth):
            case (ReactionElement.Earth, ReactionElement.Water):
                _mireReaction.Execute(context);
                break;
            case (ReactionElement.Water, ReactionElement.Wind):
            case (ReactionElement.Wind, ReactionElement.Water):
                _tempestReaction.Execute(context);
                break;
            case (ReactionElement.Earth, ReactionElement.Earth):
                _petrifyReaction.Execute(context);
                break;
            case (ReactionElement.Wind, ReactionElement.Wind):
                _cycloneReaction.Execute(context);
                break;
            case (ReactionElement.Earth, ReactionElement.Wind):
            case (ReactionElement.Wind, ReactionElement.Earth):
                _sandstormReaction.Execute(context);
                break;
        }
    }

    /// 이동과 적중 처리를 중단하고 탄환 오브젝트를 제거한다.
    private void DestroyBullet(BulletEndReason? reason = null)
    {
        if (_runtime != null)
            _runtime.Phase = BulletPhase.Ended;
        _isInitialized = false;
        if (_movement != null) _movement.EndRequested -= RequestEnd;
        Destroy(gameObject);
        if (reason.HasValue) Ended?.Invoke(reason.Value);
    }
}
