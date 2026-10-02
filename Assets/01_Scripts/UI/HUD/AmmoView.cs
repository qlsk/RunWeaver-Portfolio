using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 탄환 칸 표시.
/// 남은 탄은 밝게, 쓴 탄은 어둡게 표시하고, 재장전 중에는 빈 칸에 재장전 진행도 게이지를 채운다.
/// </summary>
[DisallowMultipleComponent]
public class AmmoView : MonoBehaviour
{
    [Header("탄환 칸 (왼쪽부터 순서대로)")]
    [SerializeField] private Image[] _bulletImages;
    [SerializeField] private Image[] _reloadGauges;

    [Header("색상")]
    [SerializeField] private Color _loadedColor = Color.white;
    [SerializeField] private Color _emptyColor = new(1f, 1f, 1f, 0.25f);

    private int _shownAmmo = -1;
    private int _shownMagazineSize = -1;
    private float _shownReloadProgress = -1f;

    private void Awake()
    {
        if (_bulletImages == null || _bulletImages.Length == 0)
        {
            Debug.LogError("[AmmoView] 탄환 이미지가 할당되지 않았습니다.", this);
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
            bullet.color = isLoaded ? _loadedColor : _emptyColor;

            Image gauge = GetReloadGauge(i);
            if (gauge != null)
            {
                gauge.fillAmount = isLoaded ? 0f : reloadProgress;
            }
        }
    }

    private Image GetReloadGauge(int index)
    {
        return _reloadGauges != null && index < _reloadGauges.Length ? _reloadGauges[index] : null;
    }
}
