using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    /*
     * TODO: Object Pooling
     */
    [Header("마탄 속도")]
    [SerializeField] private float _bulletSpeed = 5f;
    [Header("마탄 데미지")]
    [SerializeField] private float _bulletDamage = 10f;
    [Header("최대 사거리")]
    [SerializeField] private float _maxRange = 10f;

    private int _hitCount;

    public event Action Spawn;
    public event Action Fired;
    public event Action Hit;
    public event Action MaxRangeReached;

    // 목표 지점 벡터
    private Vector2 _targetDirection;
    private Vector2 _firePosition;

    public void Initialize(Vector2 targetPosition)
    {
        _targetDirection = targetPosition.normalized;
        // 발사
        Fired?.Invoke();
    }

    private void Awake()
    {
        _firePosition = transform.position;
        // 생성
        Spawn?.Invoke();
    }

    private void Update()
    {
        BulletMove();
        CheckMaxRange();
    }

    private void BulletMove()
    {
        transform.Translate(_bulletSpeed * Time.deltaTime * _targetDirection, Space.World);
    }

    public void CheckMaxRange()
    {
        if (Vector2.Distance(transform.position, _firePosition) == _maxRange)
        {
            // 최대 사거리 도달
            MaxRangeReached?.Invoke();

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();

        if (enemyHealth == null) return;

        enemyHealth.TakeDamage(_bulletDamage);

        // hit 카운트 추가(관통)
        _hitCount++;

        // 적 충돌
        Hit?.Invoke();
        // 관통이 아니면 DestroyBullet() 추가
    }

    public void DestroyBullet()
    {
        Destroy(gameObject);
    }
}
