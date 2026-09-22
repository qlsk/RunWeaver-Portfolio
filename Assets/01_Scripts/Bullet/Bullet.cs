using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    /*
     * TODO: Object Pooling
     */
    [Header("마탄 속도")]
    [SerializeField] private float _bulletSpeed = 5f;
    [Header("마탄 데미지")]
    [SerializeField] private float _bulletDamage = 10f;

    // 목표 지점
    private Vector2 _targetPosition;
    // 목표 지점 벡터
    private Vector2 _targetDirection;

    private void Start()
    {
        // PlayerInputHandler로부터 목표 지점 좌표(Screen Space)를 받아온 후, 받아온 좌표를 World Space로 변환
        _targetPosition = Camera.main.ScreenToWorldPoint(_playerInputHandler.MousePosition);
        // 목표 지점 좌표 - 총구 좌표 벡터 정규화
        _targetDirection = (_targetPosition - (Vector2)transform.position).normalized;
    }

    private void Update()
    {
        BulletMove();
    }

    private void BulletMove()
    {
        transform.Translate(_bulletSpeed * Time.deltaTime * _targetDirection);
    }
}
