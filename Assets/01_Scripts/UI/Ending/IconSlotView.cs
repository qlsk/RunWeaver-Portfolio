using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이콘 하나를 표시하는 칸 (ActivatedRuneSlot 프리팹). 비어 있으면 아이콘을 숨긴다.
/// </summary>
[DisallowMultipleComponent]
public class IconSlotView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;

    private Sprite _defaultIcon;
    private bool _isInitialized;

    /// <param name="icon">null이면 프리팹에 지정된 이미지를 그대로 사용한다.</param>
    public void Show(Sprite icon)
    {
        Initialize();

        if (_iconImage == null)
        {
            return;
        }

        _iconImage.enabled = true;
        _iconImage.sprite = icon != null ? icon : _defaultIcon;
    }

    public void ShowEmpty()
    {
        Initialize();

        if (_iconImage != null)
        {
            _iconImage.enabled = false;
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_iconImage != null)
        {
            _defaultIcon = _iconImage.sprite;
        }
    }
}
