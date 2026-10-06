using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private const float OwnerArrivalDistance = 0.1f;
    private const float ReverseSteeringDelay = 0.1f;

    private enum BulletEndReason
    {
        Hit,
        MaxRange,
        OwnerReached,
        OwnerLost
    }

    [FoldoutGroup("References"), SerializeField] private OverHeatReaction _overHeatReaction;
    [FoldoutGroup("References"), SerializeField] private SteamReaction _steamReaction;
    [FoldoutGroup("References"), SerializeField] private MoltenReaction _moltenReaction;
    [FoldoutGroup("References"), SerializeField] private FirestormReaction _firestormReaction;

    [Header("Cast Effect")]
    [SerializeField] private ParticleSystem _castEffectPrefab;
    [SerializeField, Min(0.01f)] private float _castEffectScale = 1f;
    [SerializeField, Min(0.1f)] private float _castEffectLifetime = 3f;

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

    /// 비행 상태에 따라 일반 이동 또는 플레이어 복귀를 처리한다.
    private void Update()
    {
        if (!_isInitialized)
            return;

        if (_attackData.Speed <= 0f)
        {
            DestroyBullet();
            return;
        }

        if (_runtime.Phase == BulletPhase.Returning)
            MoveReturning();
        else if (_runtime.Phase == BulletPhase.Flying)
            MoveFlying();
    }

    private void MoveFlying()
    {
        float remainingDistance = Mathf.Max(0f, _attackData.Range - _runtime.TravelDistance);
        float moveDistance = Mathf.Min(_attackData.Speed * Time.deltaTime, remainingDistance);

        transform.position += (Vector3)(_runtime.Direction * moveDistance);
        _runtime.TravelDistance += moveDistance;

        if (_runtime.TravelDistance >= _attackData.Range)
            RequestEnd(BulletEndReason.MaxRange);
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
            _runtime.ReverseSteeringRemaining -= reverseTime;
            deltaTime -= reverseTime;
        }

        if (deltaTime > 0f)
        {
            offset = owner.position - transform.position;
            _runtime.Direction = offset.normalized;
            float distance = Mathf.Min(_attackData.Speed * deltaTime, offset.magnitude);
            transform.position += (Vector3)(_runtime.Direction * distance);
            UpdateRotation();
        }

        if (((Vector2)(owner.position - transform.position)).sqrMagnitude <=
            OwnerArrivalDistance * OwnerArrivalDistance)
            RequestEnd(BulletEndReason.OwnerReached);
    }

    /// 중복 적중을 막고 Pierce 또는 최초 Reverse 발동 시 비행을 유지한다.
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
            reversedThisHit = ApplyReverse();
        }
        finally
        {
            _processingHit = false;
            if (!isPierce && !reversedThisHit)
                RequestEnd(BulletEndReason.Hit);
        }
    }

    /// 탄환의 전체 생명주기에서 한 번만 반전하고 이번 적중의 발동 여부를 반환한다.
    private bool ApplyReverse()
    {
        if (!_isInitialized || !_attackData.Configuration.IsReverse || _runtime.ReverseUsed)
            return false;

        _runtime.ReverseUsed = true;
        _runtime.Direction = -_runtime.Direction;
        if (_runtime.Phase == BulletPhase.Returning)
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

    /// 원래 종료 시점에 한 번만 복귀로 전환하고, 복귀 중 종료 요청은 최종 종료한다.
    private void RequestEnd(BulletEndReason reason)
    {
        if (!_isInitialized)
            return;

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
