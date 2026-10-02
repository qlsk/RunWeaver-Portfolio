using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RuneStoneIconCatalog", menuName = "Scriptable Objects/RuneStoneIconCatalog")]
public class RuneStoneIconCatalog : ScriptableObject
{
    [SerializeField] private List<RuneStoneIconData> _icons;
    private Dictionary<string, RuneStoneIconData> _iconsById;

    private void OnEnable()
    {
        _iconsById = new Dictionary<string, RuneStoneIconData>();
        foreach (RuneStoneIconData icon in _icons)
        {
            if (!_iconsById.TryAdd(icon.Id, icon))
            {
                throw new InvalidOperationException("An icon with the same id already exists.");
            }
        }
    }

    public RuneStoneIconData GetIcon(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
        }
        if (!_iconsById.TryGetValue(id, out RuneStoneIconData icon))
        {
            throw new ArgumentException("Unknown rune icon id: " + id);
        }
        return _iconsById[id];
    }
}
