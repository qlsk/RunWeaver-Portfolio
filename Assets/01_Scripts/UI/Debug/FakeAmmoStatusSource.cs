using UnityEngine;

/// <summary>
/// [테스트 전용] HUD 탄환 칸을 확인하기 위한 가짜 탄창.
/// 기획서 5-3 기준(탄창 10발, 재장전 2초, 탄창 소진 시 자동 재장전)으로 동작한다.
///
/// 테스트 키
/// 6: 1발 발사
/// 7: 수동 재장전
/// </summary>
public class FakeAmmoStatusSource : AmmoStatusSource
{
    [SerializeField, Min(1)] private int _magazineSize = 10;
    [SerializeField, Min(0.1f)] private float _reloadTime = 2f;

    [Header("테스트 키")]
    [SerializeField] private KeyCode _fireKey = KeyCode.Alpha6;
    [SerializeField] private KeyCode _reloadKey = KeyCode.Alpha7;

    private int _currentAmmo;
    private bool _isReloading;
    private float _reloadElapsed;

    public override int CurrentAmmo => _currentAmmo;
    public override int MagazineSize => _magazineSize;
    public override bool IsReloading => _isReloading;
    public override float ReloadProgress => _isReloading ? Mathf.Clamp01(_reloadElapsed / _reloadTime) : 0f;

    private void Awake()
    {
        _currentAmmo = _magazineSize;
    }

    private void Update()
    {
        if (_isReloading)
        {
            _reloadElapsed += Time.deltaTime;
            if (_reloadElapsed >= _reloadTime)
            {
                _currentAmmo = _magazineSize;
                _isReloading = false;
            }

            return;
        }

        if (Input.GetKeyDown(_fireKey))
        {
            Fire();
        }

        if (Input.GetKeyDown(_reloadKey))
        {
            StartReload();
        }
    }

    [ContextMenu("Test/Fire")]
    private void Fire()
    {
        if (_isReloading || _currentAmmo <= 0)
        {
            return;
        }

        _currentAmmo--;
        if (_currentAmmo == 0)
        {
            StartReload();
        }
    }

    [ContextMenu("Test/Reload")]
    private void StartReload()
    {
        if (_isReloading || _currentAmmo >= _magazineSize)
        {
            return;
        }

        _isReloading = true;
        _reloadElapsed = 0f;
    }
}
