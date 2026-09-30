using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    /*
     * TODO: Object Pooling
     */
    [Header("마탄 쿨타임")]
    [SerializeField] private float _bulletFireRate = 1f;
    // 마탄 쿨타이머(탄 하나 하나의 쿨 타임)
    private float _bulletFireCoolTimer;
    public float BulletFireCoolTimer => _bulletFireCoolTimer;

    [Header("탄창 크기")]
    [SerializeField] private int _magazineSize = 10;
    public int MagazineSize => _magazineSize;
    // 현재 탄 수
    private int _currentAmmo;
    public int CurrentAmmo => _currentAmmo;

    [Header("재장전 시간")]
    [SerializeField] private float _reloadTime = 2f;
    // 재장전 여부
    private bool _isReloading;
    public bool IsReloading => _isReloading;

    // 재장전 진행도
    private float _reloadTimer;
    public float ReloadTimer => _reloadTimer;

    [Header("마탄 프리팹")]
    [SerializeField] private Bullet _bulletPrefab;
    [Header("총구 위치")]
    [SerializeField] private GameObject _firePosition;
    [Header("기본 탄환 능력치")]
    [SerializeField, Min(0f)] private float _bulletDamage = 10f;
    [SerializeField, Min(0.01f)] private float _bulletSpeed = 5f;
    [SerializeField, Min(0f)] private float _bulletRange = 10f;

    private PlayerInputHandler _playerInputHandler;

    public void Start()
    {
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _currentAmmo = _magazineSize;
        _bulletFireCoolTimer = _bulletFireRate;
    }

    private void Update()
    {
        _bulletFireCoolTimer += Time.deltaTime;

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
        Vector2 targetDirection = (targetPosition - (Vector2)transform.position).normalized;

        float bulletRoationZ = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        // 총알 생성
        Bullet bullet = Instantiate(_bulletPrefab, _firePosition.transform.position,
            Quaternion.Euler(0, 0, bulletRoationZ));

        // 이 발사의 기본 능력치. Power/Speed/Range 보정은 Initialize 전에 반영한다.
        var stats = new BulletStats
        {
            Damage = _bulletDamage,
            Speed = _bulletSpeed,
            Range = _bulletRange
        };
        // 현재는 아래 생성자에서 룬을 직접 지정한다. 장착 룬 자동 해석은 아직 연결되지 않았다.
        // 장착 시스템 연결 시 해당 시스템이 만든 Config로 교체한다.
        var effects = new BulletEffectConfig(
            endEffect: BulletEndEffect.Orbit,
            pierce: true,
            seek: true,
            reverse: true
        );
        // 플레이어 Transform은 Return/Orbit의 기준이다. 효과 이벤트 구독은 Initialize 이후에 한다.
        bullet.Initialize(stats, effects, transform, targetDirection);

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