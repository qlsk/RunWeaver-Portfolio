using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    /*
     * TODO: Object Pooling
     */
    [Header("마탄 쿨타임")]
    [SerializeField] private float _bulletFireRate = 1f;
    [Header("탄창 크기")]
    [SerializeField] private int _magazineSize = 4;
    [Header("재장전 시간")]
    [SerializeField] private float _reloadTime = 2f;
    [Header("마탄 프리팹")]
    [SerializeField] private Bullet _bulletPrefab;
    [Header("총구 위치")]
    [SerializeField] private GameObject _firePosition;

    private PlayerInputHandler _playerInputHandler;

    // 마탄 쿨타이머
    private float _bulletFireCoolTimer;
    private int _currentAmmo;
    private bool _isReloading;
    private float _reloadTimer;

    public void Start()
    {
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _currentAmmo = _magazineSize;
        _bulletFireCoolTimer = _bulletFireRate;
    }

    private void Update()
    {
        _bulletFireCoolTimer += Time.deltaTime;

        // 재장전 시작 후 1초가 지나면 탄창을 채움
        if (_isReloading)
        {
            _reloadTimer += Time.deltaTime;
            if (_reloadTimer < _reloadTime) return;

            _currentAmmo = _magazineSize;
            _isReloading = false;
        }

        if (_playerInputHandler.IsReloadPressed || _currentAmmo == 0)
        {
            Reload();
        }

        if (_isReloading) return;

        // 마우스 왼쪽 버튼을 누르고 있고 마탄 쿨타임이 다 지났다면
        if (_playerInputHandler.IsAttackHeld && _bulletFireCoolTimer >= _bulletFireRate)
        {
            Fire();
        }
    }

    public void Fire()
    {
        if (_isReloading || _currentAmmo <= 0 || _bulletFireCoolTimer < _bulletFireRate) return;

        // 마우스 Screen 좌표 -> World 좌표
        Vector2 targetPosition = Camera.main.ScreenToWorldPoint(_playerInputHandler.MousePosition);

        // 총구 -> 마우스 방향
        Vector2 targetDirection = (targetPosition - (Vector2)_firePosition.transform.position).normalized;

        float bulletRoationZ = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        // 총알 생성
        Bullet bullet = Instantiate(_bulletPrefab, _firePosition.transform.position, Quaternion.Euler(0, 0, bulletRoationZ));

        // 총알에게 발사 방향 전달
        bullet.Initialize(targetDirection);

        _currentAmmo--;
        // 발사 간격을 마지막 발사 시점부터 측정
        _bulletFireCoolTimer = 0f;
    }

    public void Reload()
    {
        if (_isReloading || _currentAmmo >= _magazineSize) return;

        _isReloading = true;
        _reloadTimer = 0f;
    }
}
