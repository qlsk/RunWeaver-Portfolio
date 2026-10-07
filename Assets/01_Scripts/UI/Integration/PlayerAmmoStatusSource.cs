using UnityEngine;

/// <summary>
/// PlayerAttack의 탄약/재장전 상태를 HUD 탄약 표시에 연결한다.
/// PlayerAttack을 지정하지 않으면(프리팹 HUD) 씬에서 찾는다. 플레이어가 늦게 생겨도 찾을 때까지 주기적으로 다시 찾는다.
/// PlayerAttack이 없는 씬(마을)에서는 탄창 크기 0으로 알려 탄환 칸을 숨긴다.
/// </summary>
public class PlayerAmmoStatusSource : AmmoStatusSource
{
    private const float SearchInterval = 1f;

    [Tooltip("비워두면 씬에서 찾는다")]
    [SerializeField] private PlayerAttack _playerAttack;

    private float _nextSearchTime;

    public override int CurrentAmmo => Attack != null ? Attack.CurrentAmmo : 0;

    public override int MagazineSize => Attack != null ? Attack.MagazineSize : 0;

    public override bool IsReloading => Attack != null && Attack.IsReloading;

    public override float ReloadProgress =>
        IsReloading && Attack.ReloadTime > 0f
            ? Mathf.Clamp01(Attack.ReloadTimer / Attack.ReloadTime)
            : 0f;

    private PlayerAttack Attack
    {
        get
        {
            if (_playerAttack == null && Time.unscaledTime >= _nextSearchTime)
            {
                _nextSearchTime = Time.unscaledTime + SearchInterval;
                _playerAttack = FindAnyObjectByType<PlayerAttack>();
            }

            return _playerAttack;
        }
    }
}
