using UnityEngine;

/// <summary>
/// 탄창 상태를 HUD 탄환 칸에 표시한다. 재장전 진행도가 매 프레임 바뀌므로 매 프레임 읽는다.
/// </summary>
[DisallowMultipleComponent]
public class AmmoHudBinder : MonoBehaviour
{
    [SerializeField] private AmmoStatusSource _source;
    [SerializeField] private AmmoView _view;

    private void Awake()
    {
        if (_source == null)
        {
            Debug.LogWarning("[AmmoHudBinder] 탄창 데이터가 할당되지 않아 탄환 칸을 갱신하지 않습니다.", this);
        }

        if (_view == null)
        {
            Debug.LogError("[AmmoHudBinder] AmmoView가 할당되지 않았습니다.", this);
        }
    }

    private void LateUpdate()
    {
        if (_source == null || _view == null)
        {
            return;
        }

        float reloadProgress = _source.IsReloading ? _source.ReloadProgress : 0f;
        _view.SetAmmo(_source.CurrentAmmo, _source.MagazineSize, reloadProgress);
    }
}
