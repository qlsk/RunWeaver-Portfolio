using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// 화면 위쪽 재료 보유량 칸 하나 (예: Prima Materia 350)
/// </summary>
[DisallowMultipleComponent]
public class ResearchResourceView : MonoBehaviour
{
    [SerializeField] private string _resourceId;
    [SerializeField] private TMP_Text _text;

    public void Show(ResearchDataSource source)
    {
        if (_text == null || source == null)
        {
            return;
        }

        int amount = source.GetResourceAmount(_resourceId);
        _text.text = $"{ResearchText.GetResourceName(_resourceId)} {amount.ToString("#,0", CultureInfo.InvariantCulture)}";
    }
}
