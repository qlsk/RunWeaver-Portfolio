using System.Collections.Generic;
using UnityEngine;

public class RuneEffectDatabase : Singleton<RuneEffectDatabase>
{
    private bool _isInitialized = false;
    private Dictionary<string, RuneEffectData> _database = new();
    private IReadOnlyList<RuneEffectData> _effects = new List<RuneEffectData>();

    public void Initialize(IReadOnlyList<RuneEffectData> effects)
    {
        if (_isInitialized) return;
        _database.Clear();
        foreach (RuneEffectData effect in effects)
        {
            if (!_database.TryAdd(effect.Id, effect))
            {
                Debug.LogError($"Duplicate effect id: {effect.Id}");
            }
        }

        _effects = new List<RuneEffectData>(_database.Values);
        _isInitialized = true;
        Debug.Log($"룬 효과 데이터베이스를 초기화했습니다.");
    }

    public IReadOnlyList<RuneEffectData> GetAll()
    {
        return _effects;
    }

    public bool TryGetById(string id, out RuneEffectData runeEffectData)
    {
        return _database.TryGetValue(id, out runeEffectData);
    }
}
