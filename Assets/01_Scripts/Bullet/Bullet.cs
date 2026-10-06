using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class Bullet : MonoBehaviour
{
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

    /// 공격 방향으로 탄환을 이동시키고 사거리에 도달하면 제거한다.
    private void Update()
    {
        if (!_isInitialized)
            return;

        float remainingDistance = Mathf.Max(0f, _attackData.Range - _runtime.TravelDistance);
        float moveDistance = Mathf.Min(_attackData.Speed * Time.deltaTime, remainingDistance);

        transform.position += (Vector3)(_runtime.Direction * moveDistance);
        _runtime.TravelDistance += moveDistance;

        if (_runtime.TravelDistance >= _attackData.Range)
            DestroyBullet();
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
                DestroyBullet();
        }
    }

    /// 탄환의 전체 생명주기에서 한 번만 반전하고 이번 적중의 발동 여부를 반환한다.
    private bool ApplyReverse()
    {
        if (!_isInitialized || !_attackData.Configuration.IsReverse || _runtime.ReverseUsed)
            return false;

        _runtime.ReverseUsed = true;
        _runtime.Direction = -_runtime.Direction;
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

    /// 이동과 적중 처리를 중단하고 탄환 오브젝트를 제거한다.
    private void DestroyBullet()
    {
        if (_runtime != null)
            _runtime.Phase = BulletPhase.Ended;
        _isInitialized = false;
        Destroy(gameObject);
    }
}
