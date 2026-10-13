using UnityEngine;

public class EnemyContactAttack : MonoBehaviour
{
    [SerializeField] private float _damage;
    [SerializeField] private float _attackCooldown;
    [Tooltip("제단을 때리는 간격(초). 플레이어 공격 간격과 따로 둔다. (여러 마리가 붙어도 제단이 순식간에 부서지지 않게)")]
    [SerializeField] private float _altarAttackCooldown = 1.5f;

    private float _nextAttackTime;
    private float _nextAltarAttackTime;
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
            (_statusEffectController != null && _statusEffectController.IsStaggered))
        {
            return;
        }

        if (collision.TryGetComponent(out PlayerHitbox hitbox) && hitbox.Health != null && !hitbox.Health.IsDead)
        {
            if (Time.time < _nextAttackTime) return;
            _nextAttackTime = Time.time + _attackCooldown;
            hitbox.Health.TakeDamage(_damage);
        }
        else
        {
            if (Time.time < _nextAltarAttackTime) return;
            DefenseAltar altar = collision.GetComponentInParent<DefenseAltar>();
            if (altar == null || !altar.isActiveAndEnabled || altar.IsDestroyed) return;
            _nextAltarAttackTime = Time.time + _altarAttackCooldown;
            altar.TakeDamage(_damage);
        }
    }
}
