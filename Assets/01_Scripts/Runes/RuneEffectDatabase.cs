using System.Collections.Generic;
using UnityEngine;

public class RuneEffectDatabase : Singleton<RuneEffectDatabase>
{
    private bool _isInitialized = false;
    private Dictionary<string, RuneEffect> _database = new();

    public void Initialize(IReadOnlyList<RuneEffect> effects)
    {
        if (_isInitialized) return;
        _database.Clear();
        foreach (RuneEffect effect in effects)
        {
            if (!_database.TryAdd(effect.Id, effect))
            {
                Debug.LogError($"Duplicate effect id: {effect.Id}");
            }
        }

        Debug.Log($"Initialized {_database.Count} rune effects.");
    }
}
