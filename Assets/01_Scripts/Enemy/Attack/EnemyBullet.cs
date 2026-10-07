using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private Vector2 _direction;
    private bool _initialized;
    [SerializeField] private float _damage = 5f;
    [SerializeField, Min(0f)] private float _speed = 2f;

    public void Initialize(Vector2 direction)
    {
        _direction = direction.normalized;
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
        if (!collision.TryGetComponent(out PlayerHitbox hitbox) || hitbox.Health == null)
            return;

        hitbox.Health.TakeDamage(_damage);
        Destroy(gameObject);
    }
}
