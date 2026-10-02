using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룬 효과 id별 아이콘 목록. (RuneStoneIconCatalog와 같은 방식)
/// DB에는 이미지 이름을 두지 않고, 룬 효과 id로 이 목록에서 아이콘을 찾는다.
/// </summary>
[CreateAssetMenu(fileName = "RuneEffectIconCatalog", menuName = "Scriptable Objects/RuneEffectIconCatalog")]
public class RuneEffectIconCatalog : ScriptableObject
{
    [SerializeField] private List<RuneEffectIconData> _icons;
    private Dictionary<string, RuneEffectIconData> _iconsById;

    private void OnEnable()
    {
        _iconsById = new Dictionary<string, RuneEffectIconData>();
        if (_icons == null)
        {
            return;
        }

        foreach (RuneEffectIconData icon in _icons)
        {
            if (!_iconsById.TryAdd(icon.Id, icon))
            {
                throw new InvalidOperationException("A rune effect icon with the same id already exists: " + icon.Id);
            }
        }
    }

    public RuneEffectIconData GetIcon(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
        }

        if (!_iconsById.TryGetValue(id, out RuneEffectIconData icon))
        {
            throw new ArgumentException("Unknown rune effect icon id: " + id);
        }

        return icon;
    }
}
