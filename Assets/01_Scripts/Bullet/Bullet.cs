using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private const string EnemyTag = "Enemy";

    [FoldoutGroup("References"), SerializeField] private OverHeatReaction _overHeatReaction;
    [FoldoutGroup("References"), SerializeField] private SteamReaction _steamReaction;

    [Header("Cast Effect")]
    [SerializeField] private ParticleSystem _castEffectPrefab;
    [SerializeField, Min(0.01f)] private float _castEffectScale = 1f;
    [SerializeField, Min(0.1f)] private float _castEffectLifetime = 3f;

    private PlayerAttackData _attackData;
    private float _travelDistance;
    private bool _isInitialized;

    /// Stores the attack data and prepares the bullet for movement.
    public void Initialize(PlayerAttackData attackData)
    {
        _attackData = attackData.WithDirection(attackData.Direction.normalized);

        _travelDistance = 0f;
        _isInitialized = true;
    }

    /// Moves the bullet and removes it when its range is exhausted.
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

    /// Applies damage and element effects to a living enemy.
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
            }

            return;
        }

        if (enemyHealth == null || enemyHealth.IsDead || !enemyHealth.isActiveAndEnabled)
            return;

        if (!enemyHealth.TryGetComponent(out EnemyStatusEffectController statusEffects))
            throw new InvalidOperationException($"EnemyStatusEffectController is missing: {enemyHealth.name}");

        statusEffects.ApplyElementEffect(configuration.PrimaryElement.Value, _attackData.Damage, _attackData.Direction);
    }

    /// Stops the bullet and removes its game object.
    private void DestroyBullet()
    {
        _isInitialized = false;
        Destroy(gameObject);
    }
}
