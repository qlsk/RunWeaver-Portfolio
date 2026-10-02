using TMPro;
using UnityEngine;

/// <summary>
/// HUD 골드 표시
/// </summary>
[DisallowMultipleComponent]
public class GoldHudView : MonoBehaviour
{
    [SerializeField] private TMP_Text _amountText;

    public void SetAmount(int amount)
    {
        if (_amountText != null)
        {
            _amountText.text = amount.ToString();
        }
    }
}
