using UnityEngine;

public class EnemyContactAttack : MonoBehaviour
{
    [SerializeField] private float _damage;
    [SerializeField] private float _attackCooldown;

    private float _nextAttackTime;
    private EnemyStatusEffectController _statusEffectController;
    private EnemyHealth _health;

    private void Awake()
    {
        _statusEffectController = GetComponentInParent<EnemyStatusEffectController>();
        _health = GetComponentInParent<EnemyHealth>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        ApplyContactDamage(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ApplyContactDamage(collision.collider);
    }

    private void ApplyContactDamage(Collider2D collision)
    {
        if ((_health != null && _health.IsDead) ||
            (_statusEffectController != null && _statusEffectController.IsStaggered) ||
            Time.time < _nextAttackTime)
        {
            return;
        }

        if (collision.TryGetComponent(out PlayerHitbox hitbox) && hitbox.Health != null && !hitbox.Health.IsDead)
        {
            _nextAttackTime = Time.time + _attackCooldown;
            hitbox.Health.TakeDamage(_damage);
        }
        else
        {
            DefenseAltar altar = collision.GetComponentInParent<DefenseAltar>();
            if (altar == null || !altar.isActiveAndEnabled || altar.IsDestroyed) return;
            _nextAttackTime = Time.time + _attackCooldown;
            altar.TakeDamage(_damage);
        }
    }
}
