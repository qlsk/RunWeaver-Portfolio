using UnityEngine;

/// <summary>
/// PlayerAttack의 탄약/재장전 상태를 HUD 탄약 표시에 연결한다.
/// PlayerAttack의 재장전 시간(_reloadTime)이 아직 공개되지 않아, 공개 전까지는 인스펙터 값으로 진행률을 계산한다.
/// </summary>
public class PlayerAmmoStatusSource : AmmoStatusSource
{
    [SerializeField] private PlayerAttack _playerAttack;

    [Tooltip("PlayerAttack의 재장전 시간과 같게 맞춘다. (PlayerAttack.ReloadTime 공개 후 제거 예정)")]
    [SerializeField, Min(0.01f)] private float _reloadTime = 2f;

    public override int CurrentAmmo => _playerAttack != null ? _playerAttack.CurrentAmmo : 0;

    public override int MagazineSize => _playerAttack != null ? _playerAttack.MagazineSize : 0;

    public override bool IsReloading => _playerAttack != null && _playerAttack.IsReloading;

    public override float ReloadProgress =>
        IsReloading ? Mathf.Clamp01(_playerAttack.ReloadTimer / _reloadTime) : 0f;

    private void Awake()
    {
        if (_playerAttack == null)
        {
            Debug.LogError("[PlayerAmmoStatusSource] PlayerAttack이 할당되지 않았습니다.", this);
        }
    }
}
