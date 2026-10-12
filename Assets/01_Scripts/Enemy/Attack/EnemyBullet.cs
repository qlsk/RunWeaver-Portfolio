using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private Vector2 _direction;
    private bool _initialized;
    private bool _hasHit;
    [SerializeField] private float _damage = 5f;
    [SerializeField, Min(0f)] private float _speed = 2f;

    public void Initialize(Vector2 direction)
    {
        _direction = direction.normalized;
        if (_direction.sqrMagnitude > 0f)
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        _initialized = true;
    }

    private void Start()
    {
        if (_initialized)
            return;

        EnemyTargetDetector targetDetector = GetComponent<EnemyTargetDetector>();
        if (targetDetector != null && targetDetector.Target != null)
        {
            Initialize(targetDetector.Target.position - transform.position);
        }
    }

    private void Update()
    {
        float moveDistance = _speed * Time.deltaTime;
        transform.position += (Vector3)(_direction * moveDistance);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasHit) return;
        if (collision.TryGetComponent(out PlayerHitbox hitbox) && hitbox.Health != null && !hitbox.Health.IsDead)
        {
            _hasHit = true;
            hitbox.Health.TakeDamage(_damage);
        }
        else
        {
            DefenseAltar altar = collision.GetComponentInParent<DefenseAltar>();
            if (altar == null || !altar.isActiveAndEnabled || altar.IsDestroyed) return;
            _hasHit = true;
            altar.TakeDamage(_damage);
        }
        Destroy(gameObject);
    }
}
