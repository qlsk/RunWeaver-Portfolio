using TMPro;
using UnityEngine;

/// <summary>
/// 탄창 상태를 HUD 탄환 칸과 남은 탄 수 텍스트에 표시한다.
/// </summary>
[DisallowMultipleComponent]
public class AmmoHudBinder : MonoBehaviour
{
    [SerializeField] private AmmoStatusSource _source;
    [SerializeField] private AmmoView _view;
    [SerializeField] private TMP_Text _bulletCountText;

    private int _shownAmmo = -1;
    private int _shownMagazineSize = -1;

    private void Awake()
    {
        if (_source == null)
        {
            Debug.LogWarning("[AmmoHudBinder] 탄창 데이터가 할당되지 않아 탄환 칸을 갱신하지 않습니다.", this);
        }

        if (_view == null && _bulletCountText == null)
        {
            Debug.LogError("[AmmoHudBinder] 탄약 표시가 할당되지 않았습니다.", this);
        }
    }

    private void LateUpdate()
    {
        if (_source == null)
        {
            return;
        }

        int currentAmmo = _source.CurrentAmmo;
        int magazineSize = _source.MagazineSize;

        if (_view != null)
        {
            float reloadProgress = _source.IsReloading ? _source.ReloadProgress : 0f;
            _view.SetAmmo(currentAmmo, magazineSize, reloadProgress);
        }

        if (_bulletCountText != null && (currentAmmo != _shownAmmo || magazineSize != _shownMagazineSize))
        {
            _shownAmmo = currentAmmo;
            _shownMagazineSize = magazineSize;
            _bulletCountText.text = magazineSize > 0 ? $"{currentAmmo} / {magazineSize}" : string.Empty;
        }
    }
}
