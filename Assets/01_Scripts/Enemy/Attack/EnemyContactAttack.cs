using UnityEngine;

public class EnemyContactAttack : MonoBehaviour
{
    [SerializeField] private float _damage;
    [SerializeField] private float _attackCooldown;

    private float _nextAttackTime;
    private EnemyStatusEffectController _statusEffectController;

    private void Awake()
    {
        _statusEffectController = GetComponentInParent<EnemyStatusEffectController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_statusEffectController.IsStaggered)
            return;

        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (Time.time < _nextAttackTime)
        {
            return;
        }

        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            return;
        }

        playerHealth.TakeDamage(_damage);

        _nextAttackTime = Time.time + _attackCooldown;
    }
}