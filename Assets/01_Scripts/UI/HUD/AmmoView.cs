using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 탄환 칸 표시.
/// 남은 탄은 채워 표시하고, 쓴 탄은 비운 뒤 재장전 진행도에 따라 다시 채운다.
/// </summary>
[DisallowMultipleComponent]
public class AmmoView : MonoBehaviour
{
    [Header("탄환 칸 (왼쪽부터 순서대로)")]
    [SerializeField] private Image[] _bulletImages;

    [Header("색상")]
    [SerializeField] private Color _loadedColor = Color.white;

    private int _shownAmmo = -1;
    private int _shownMagazineSize = -1;
    private float _shownReloadProgress = -1f;

    private void Awake()
    {
        foreach (Image bullet in _bulletImages)
        {
            if (bullet != null)
                bullet.type = Image.Type.Filled;
        }
    }

    /// <param name="reloadProgress">재장전 진행도 (0 ~ 1). 재장전 중이 아니면 0</param>
    public void SetAmmo(int currentAmmo, int magazineSize, float reloadProgress)
    {
        if (_bulletImages == null)
        {
            return;
        }

        reloadProgress = Mathf.Clamp01(reloadProgress);
        if (currentAmmo == _shownAmmo && magazineSize == _shownMagazineSize &&
            Mathf.Approximately(reloadProgress, _shownReloadProgress))
        {
            return;
        }

        _shownAmmo = currentAmmo;
        _shownMagazineSize = magazineSize;
        _shownReloadProgress = reloadProgress;

        for (int i = 0; i < _bulletImages.Length; i++)
        {
            Image bullet = _bulletImages[i];
            if (bullet == null)
            {
                continue;
            }

            bool isInMagazine = i < magazineSize;
            bullet.gameObject.SetActive(isInMagazine);
            if (!isInMagazine)
            {
                continue;
            }

            bool isLoaded = i < currentAmmo;
            bullet.color = _loadedColor;
            bullet.fillAmount = isLoaded ? 1f : reloadProgress;
        }
    }

}
