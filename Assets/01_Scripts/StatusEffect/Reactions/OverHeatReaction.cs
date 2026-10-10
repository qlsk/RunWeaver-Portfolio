using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// 적중 지점에 폭발 피해를 적용하고 독립적으로 유지되는 잔불 장판을 생성한다.
public class OverHeatReaction : MonoBehaviour
{
    // [FoldoutGroup("References"), SerializeField] private ParticleSystem _explosionEffectPrefab;
    [FoldoutGroup("References"), SerializeField] private EmberArea _emberAreaPrefab;

    [FoldoutGroup("Explosion Settings"), SerializeField] private float _explosionRadius = 2f;
    [FoldoutGroup("Explosion Settings"), SerializeField] private float _explosionDamageMultiplier = 1f;
    [FoldoutGroup("Explosion Settings"), SerializeField] private float _explosionEffectLifetime = 3f;

    [FoldoutGroup("Ember Settings"), SerializeField] private float _emberDamageMultiplier = 0.2f;
    [FoldoutGroup("Ember Settings"), SerializeField] private float _emberTickInterval = 1f;
    [FoldoutGroup("Ember Settings"), SerializeField] private float _emberDuration = 3f;

    public void Execute(ReactionContext context)
    {
        float explosionDamage = context.BulletDamage * _explosionDamageMultiplier;
        float emberTickDamage = context.BulletDamage * _emberDamageMultiplier;

        ApplyExplosionDamage(context.HitPosition, explosionDamage);

        // ParticleSystem explosionEffect = Instantiate(_explosionEffectPrefab, context.HitPosition, Quaternion.identity);
        // explosionEffect.Play(true);
        // Destroy(explosionEffect.gameObject, _explosionEffectLifetime);

        EmberArea emberArea = Instantiate(_emberAreaPrefab, context.HitPosition, Quaternion.identity);
        emberArea.Initialize(emberTickDamage, _emberTickInterval, _emberDuration);
    }

    private void ApplyExplosionDamage(Vector2 hitPosition, float damage)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(hitPosition, _explosionRadius);
        HashSet<EnemyHealth> enemies = new();
        foreach (Collider2D collider in colliders)
        {
            if (!collider.TryGetComponent(out EnemyHealth enemy))
                continue;
            enemies.Add(enemy);
        }

        IEnumerable<EnemyHealth> damageableEnemies =
            enemies.Where(enemy => enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled);
        foreach (EnemyHealth enemy in damageableEnemies)
        {
            enemy.TakeDamage(damage);
        }
    }
}
