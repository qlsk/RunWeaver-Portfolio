using UnityEngine;

public class PlayerFire : MonoBehaviour
{
    /*
     * TODO: Object Pooling
     */
    [Header("마탄 쿨타임")] [SerializeField] private float _bulletFireRate = 1f;
    [Header("마탄 프리팹")] [SerializeField] private Bullet _bulletPrefab;
    [Header("총구 위치")] [SerializeField] private GameObject _firePosition;

    private PlayerInputHandler _playerInputHandler;

    // 마탄 쿨타이머
    private float _bulletFireCoolTimer = 0f;

    public void Start()
    {
        _playerInputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        // 마탄 쿨타임이 다 지났다면
        if (_bulletFireCoolTimer >= _bulletFireRate)
        {
            Fire();
            // 마탄 쿨타이머 초기화
            _bulletFireCoolTimer = 0f;
        }

        _bulletFireCoolTimer += Time.deltaTime;
    }

    public void Fire()
    {
        // 마우스 Screen 좌표 -> World 좌표
        Vector2 targetPosition = Camera.main.ScreenToWorldPoint(_playerInputHandler.MousePosition);

        // 총구 -> 마우스 방향
        Vector2 targetDirection = (targetPosition - (Vector2)transform.position).normalized;

        // 총알 생성
        Bullet bullet = Instantiate(_bulletPrefab, _firePosition.transform.position, transform.rotation);

        // 총알에게 발사 방향 전달
        bullet.Initialize(targetDirection);
    }
}
