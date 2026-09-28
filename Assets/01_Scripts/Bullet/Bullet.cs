using UnityEngine;

public class Bullet : MonoBehaviour
{
    /*
     * TODO: Object Pooling
     * 생명 주기 - 생성, 발사, 충돌, 최대 사거리 도달
     * 생성 발사 충돌 or 최대 사거리 도달
     */
    [Header("마탄 속도")][SerializeField] private float _bulletSpeed = 5f;
    [Header("마탄 데미지")][SerializeField] private float _bulletDamage = 10f;

    // 목표 지점 벡터
    private Vector2 _targetDirection;

    public void Initialize(Vector2 targetPosition)
    {
        _targetDirection = targetPosition.normalized;
    }

    private void Update()
    {
        BulletMove();
    }

    private void BulletMove()
    {
        transform.Translate(_bulletSpeed * Time.deltaTime * _targetDirection, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();

        if (enemyHealth == null) return;

        enemyHealth.TakeDamage(_bulletDamage);

        Destroy(gameObject);
    }
}
