using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 탄환 칸 표시.
/// 남은 탄은 채워 표시하고, 쓴 탄은 비운 뒤 재장전 진행도에 따라 다시 채운다.
/// 탄창 크기가 늘면 탄환 칸을 부족한 만큼 생성한다.
/// </summary>
[DisallowMultipleComponent]
public class AmmoView : MonoBehaviour
{
    private readonly struct BulletSlot
    {
        public readonly GameObject Root;
        public readonly Image Image;

        public BulletSlot(GameObject root, Image image)
        {
            Root = root;
            Image = image;
        }
    }

    [Header("탄환 칸 생성")]
    [SerializeField] private RectTransform _bulletContainer;
    [SerializeField] private RectTransform _bulletPrefab;
    [SerializeField, Min(0)] private int _defaultAmmoSize = 10;

    [Header("색상")]
    [SerializeField] private Color _loadedColor = Color.white;

    private readonly List<BulletSlot> _bullets = new List<BulletSlot>();
    private bool _initialized;
    private bool _ready;

    private int _shownAmmo = -1;
    private int _shownMagazineSize = -1;
    private float _shownReloadProgress = -1f;

    private void Awake()
    {
        EnsureInitialized();
    }

    /// <summary>참조와 프리팹을 한 번만 검증한다. 유효할 때만 칸을 생성한다.</summary>
    private bool EnsureInitialized()
    {
        if (_initialized)
        {
            return _ready;
        }

        _initialized = true;
        if (_bulletContainer == null || _bulletPrefab == null)
        {
            Debug.LogError("[AmmoView] 탄환 칸 컨테이너 또는 프리팹이 할당되지 않았습니다.", this);
            return false;
        }

        AmmoBulletSlot prefabSlot = _bulletPrefab.GetComponent<AmmoBulletSlot>();
        if (prefabSlot == null || prefabSlot.Fill == null)
        {
            Debug.LogError("[AmmoView] 탄환 프리팹에 AmmoBulletSlot이 없거나 Fill이 연결되지 않았습니다.", _bulletPrefab);
            return false;
        }

        _ready = true;
        EnsureBulletCount(_defaultAmmoSize);
        return true;
    }

    /// <summary>탄환 칸이 count개 이상이 되도록 부족한 만큼 생성한다. 줄이지는 않는다.</summary>
    private void EnsureBulletCount(int count)
    {
        while (_bullets.Count < count)
        {
            RectTransform bullet = Instantiate(_bulletPrefab, _bulletContainer);
            _bullets.Add(new BulletSlot(bullet.gameObject, bullet.GetComponent<AmmoBulletSlot>().Fill));
        }
    }

    /// <param name="reloadProgress">재장전 진행도 (0 ~ 1). 재장전 중이 아니면 0</param>
    public void SetAmmo(int currentAmmo, int magazineSize, float reloadProgress)
    {
        if (!EnsureInitialized())
        {
            return;
        }

        reloadProgress = Mathf.Clamp01(reloadProgress);
        if (currentAmmo == _shownAmmo && magazineSize == _shownMagazineSize &&
            Mathf.Approximately(reloadProgress, _shownReloadProgress))
        {
            return;
        }

        EnsureBulletCount(magazineSize);

        _shownAmmo = currentAmmo;
        _shownMagazineSize = magazineSize;
        _shownReloadProgress = reloadProgress;

        for (int i = 0; i < _bullets.Count; i++)
        {
            BulletSlot slot = _bullets[i];
            bool isInMagazine = i < magazineSize;
            slot.Root.SetActive(isInMagazine);
            if (!isInMagazine)
            {
                continue;
            }

            bool isLoaded = i < currentAmmo;
            slot.Image.color = _loadedColor;
            slot.Image.fillAmount = isLoaded ? 1f : reloadProgress;
        }
    }
}
