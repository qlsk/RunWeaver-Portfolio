using UnityEngine;

/// <summary>
/// HUD 탄환 표시가 읽는 탄창 상태.
/// 현재 PlayerAttack은 탄 수와 재장전 상태를 외부에 공개하지 않으므로,
/// 공개되면 이 클래스를 상속해 PlayerAttack 값을 전달하는 어댑터를 만든다.
/// </summary>
public abstract class AmmoStatusSource : MonoBehaviour
{
    public abstract int CurrentAmmo { get; }
    public abstract int MagazineSize { get; }
    public abstract bool IsReloading { get; }

    /// <summary>
    /// 재장전 진행도 (0 ~ 1). 재장전 중이 아니면 0
    /// </summary>
    public abstract float ReloadProgress { get; }
}
