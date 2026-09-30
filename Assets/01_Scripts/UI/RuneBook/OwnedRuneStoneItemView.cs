using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보유 룬석 목록의 한 줄 (아이콘, 이름, 개수)
/// </summary>
[DisallowMultipleComponent]
public class OwnedRuneStoneItemView : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private string _countFormat = "x{0}";

    public void Show(RuneStoneStackData runeStone)
    {
        if (runeStone == null)
        {
            return;
        }

        if (_iconImage != null && runeStone.Icon != null)
        {
            _iconImage.sprite = runeStone.Icon;
        }

        if (_nameText != null)
        {
            _nameText.text = FormatName(runeStone.Id);
        }

        if (_countText != null)
        {
            _countText.text = string.Format(_countFormat, runeStone.Quantity);
        }
    }

    private static string FormatName(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(id[0]) + id.Substring(1);
    }
}
