using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private const string EnemyTag = "Enemy";

    [FoldoutGroup("References"), SerializeField] private OverHeatReaction _overHeatReaction;
    [FoldoutGroup("References"), SerializeField] private SteamReaction _steamReaction;
    [FoldoutGroup("References"), SerializeField] private MoltenReaction _moltenReaction;
    [FoldoutGroup("References"), SerializeField] private FirestormReaction _firestormReaction;
    [FoldoutGroup("References"), SerializeField] private DelugeReaction _delugeReaction;

    [Header("Cast Effect")]
    [SerializeField] private ParticleSystem _castEffectPrefab;
    [SerializeField, Min(0.01f)] private float _castEffectScale = 1f;
    [SerializeField, Min(0.1f)] private float _castEffectLifetime = 3f;

    // Attack data
    private PlayerAttackData _attackData;

    // Runtime state
    private float _travelDistance;
    private bool _isInitialized;

    /// 공격 데이터를 저장하고 이동 거리와 적중 처리 상태를 초기화한다.
    public void Initialize(PlayerAttackData attackData)
    {
        _attackData = attackData.WithDirection(attackData.Direction.normalized);

        _travelDistance = 0f;
        _isInitialized = true;
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

        float remainingDistance = _attackData.Range - _travelDistance;
        float moveDistance = Mathf.Min(_attackData.Speed * Time.deltaTime, remainingDistance);

        transform.position += (Vector3)(_attackData.Direction * moveDistance);
        _travelDistance += moveDistance;

        if (_travelDistance >= _attackData.Range)
            DestroyBullet();
    }

    /// 적중 중복을 차단하고 직접 피해와 원소 효과를 적용한 뒤 탄환을 제거한다.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isInitialized)
            return;

        if (!collision.CompareTag(EnemyTag))
            return;

        if (!collision.TryGetComponent(out EnemyHealth enemyHealth))
            throw new InvalidOperationException($"EnemyHealth Component is missing: {collision.name}");

        if (enemyHealth == null || enemyHealth.IsDead || !enemyHealth.isActiveAndEnabled)
            return;

        // Prevent repeated hits before destruction is processed.
        _isInitialized = false;
        Vector2 hitPosition = collision.ClosestPoint(transform.position);
        enemyHealth.TakeDamage(_attackData.Damage);

        ApplyElementEffect(enemyHealth, hitPosition);

        DestroyBullet();
    }

    /// 두 원소의 반응을 실행하거나 살아 있는 적에게 단일 원소 효과를 적용한다.
    private void ApplyElementEffect(EnemyHealth enemyHealth, Vector2 hitPosition)
    {
        RuneEffectConfiguration configuration = _attackData.Configuration;
        if (!configuration.PrimaryElement.HasValue)
            return;

        if (configuration.SecondaryElement.HasValue)
        {
            switch (configuration.PrimaryElement.Value, configuration.SecondaryElement.Value)
            {
                case (ReactionElement.Fire, ReactionElement.Fire):
                    _overHeatReaction.Execute(hitPosition, _attackData.Damage);
                    break;
                case (ReactionElement.Fire, ReactionElement.Water):
                case (ReactionElement.Water, ReactionElement.Fire):
                    _steamReaction.Execute(hitPosition, _attackData.Damage);
                    break;
                case (ReactionElement.Fire, ReactionElement.Earth):
                case (ReactionElement.Earth, ReactionElement.Fire):
                    _moltenReaction.Execute(hitPosition, _attackData.Damage);
                    break;
                case (ReactionElement.Fire, ReactionElement.Wind):
                case (ReactionElement.Wind, ReactionElement.Fire):
                    _firestormReaction.Execute(hitPosition, _attackData.Damage);
                    break;
                case (ReactionElement.Water, ReactionElement.Water):
                    _delugeReaction.Execute(hitPosition, _attackData.Direction);
                    break;
            }

            return;
        }

        if (enemyHealth == null || enemyHealth.IsDead || !enemyHealth.isActiveAndEnabled)
            return;

        if (!enemyHealth.TryGetComponent(out EnemyStatusEffectController statusEffects))
            throw new InvalidOperationException($"EnemyStatusEffectController is missing: {enemyHealth.name}");

        statusEffects.ApplyElementEffect(configuration.PrimaryElement.Value, _attackData.Damage, _attackData.Direction);
    }

    /// 이동과 적중 처리를 중단하고 탄환 오브젝트를 제거한다.
    private void DestroyBullet()
    {
        _isInitialized = false;
        Destroy(gameObject);
    }
}
