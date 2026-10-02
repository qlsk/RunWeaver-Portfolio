using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아이콘과 개수를 표시하는 칸 (SlotWithCount 프리팹)
/// </summary>
[DisallowMultipleComponent]
public class CountSlotView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _countText;

    /// <param name="icon">null이면 프리팹에 지정된 이미지를 그대로 사용한다.</param>
    public void Show(Sprite icon, int count)
    {
        gameObject.SetActive(true);

        if (_iconImage != null && icon != null)
        {
            _iconImage.sprite = icon;
        }

        if (_countText != null)
        {
            _countText.text = count.ToString();
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
