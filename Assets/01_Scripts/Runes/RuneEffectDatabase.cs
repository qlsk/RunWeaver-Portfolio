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

        Debug.Log($"룬 효과 데이터베이스를 초기화했습니다.");
    }

    public IReadOnlyList<RuneEffect> GetAll()
    {
        return new List<RuneEffect>(_database.Values);
    }

    public bool TryGetById(string id, out RuneEffect runeEffect)
    {
        return _database.TryGetValue(id, out runeEffect);
    }
}
