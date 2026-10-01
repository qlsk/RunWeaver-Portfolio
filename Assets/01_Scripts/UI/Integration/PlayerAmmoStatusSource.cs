using UnityEngine;

/// <summary>
/// PlayerAttack의 탄약/재장전 상태를 HUD 탄약 표시에 연결한다.
/// </summary>
public class PlayerAmmoStatusSource : AmmoStatusSource
{
    [SerializeField] private PlayerAttack _playerAttack;

    public override int CurrentAmmo => _playerAttack != null ? _playerAttack.CurrentAmmo : 0;

    public override int MagazineSize => _playerAttack != null ? _playerAttack.MagazineSize : 0;

    public override bool IsReloading => _playerAttack != null && _playerAttack.IsReloading;

    public override float ReloadProgress =>
        IsReloading && _playerAttack.ReloadTime > 0f
            ? Mathf.Clamp01(_playerAttack.ReloadTimer / _playerAttack.ReloadTime)
            : 0f;

    private void Awake()
    {
        if (_playerAttack == null)
        {
            Debug.LogError("[PlayerAmmoStatusSource] PlayerAttack이 할당되지 않았습니다.", this);
        }
    }
}
