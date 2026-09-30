using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private float _range;
    private float _travelDistance;
    private Vector2 _direction;
    private bool _initialized;
    private ElementType _mainElement;

    /// <summary>탄환의 기본 능력치와 발사 방향, 주력 원소를 설정한다.</summary>
    public void Initialize(float damage, float speed, float range, Vector2 direction, ElementType mainElement)
    {
        _damage = damage;
        _speed = speed;
        _range = range;
        _direction = direction.normalized;
        _mainElement = mainElement;

        _travelDistance = 0f;
        _initialized = true;
    }

    /// <summary>발사 방향으로 이동하고 최대 사거리에 도달하면 소멸한다.</summary>
    private void Update()
    {
        if (!_initialized)
            return;

        float remainingDistance = _range - _travelDistance;
        float moveDistance = Mathf.Min(_speed * Time.deltaTime, remainingDistance);

        transform.position += (Vector3)(_direction * moveDistance);
        _travelDistance += moveDistance;

        if (_travelDistance >= _range)
            End();
    }

    /// <summary>살아 있는 적에게 적중하면 피해를 적용하고 소멸한다.</summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_initialized)
            return;

        var enemyHealth = collision.GetComponentInParent<EnemyHealth>();
        var enemy = collision.GetComponent<Enemy>();
        if (enemyHealth == null || enemyHealth.IsDead || !enemyHealth.isActiveAndEnabled)
            return;

        // Destroy가 처리되기 전 중복 충돌로 피해가 반복되는 것을 막는다.
        _initialized = false;
        enemyHealth.TakeDamage(_damage);

        if (_mainElement == ElementType.Fire)
        {
            enemy.Burn.Activate(_damage);
        }
        Destroy(gameObject);
    }

    /// <summary>탄환의 동작을 중지하고 게임 오브젝트를 제거한다.</summary>
    private void End()
    {
        _initialized = false;
        Destroy(gameObject);
    }
}