using UnityEngine;

public class Bullet : MonoBehaviour
{
    /*
     * TODO: Object Pooling
     */
    [Header("마탄 속도")] [SerializeField] private float _bulletSpeed = 5f;
    [Header("마탄 데미지")] [SerializeField] private float _bulletDamage = 10f;

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
        transform.Translate(_bulletSpeed * Time.deltaTime * _targetDirection);
    }

    // Test용 현재는 모든 총알이 Player에게 데미지
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            collision.GetComponent<PlayerHealth>().TakeDamage(_bulletDamage);
        }
    }
}
