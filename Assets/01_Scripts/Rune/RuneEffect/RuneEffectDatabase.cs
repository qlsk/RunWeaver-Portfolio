using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RuneEffectDatabase : Singleton<RuneEffectDatabase>
{
    [Header("References")]
    [SerializeField] private RuneDefinitionLoader _loader;

    [Header("Data")]
    [SerializeField] private List<RuneEffectDefinition> _runeEffects;

    private Dictionary<string, RuneEffectDefinition> _runeEffectsById;

    private bool _databaseReady;

    public bool DatabaseReady => _databaseReady;

    public async UniTask Initialize()
    {
        IReadOnlyList<RuneEffectDefinition> loadedRuneEffects = await _loader.LoadRuneEffectDefinitions();

        List<RuneEffectDefinition> runeEffects = new(loadedRuneEffects.Count);
        Dictionary<string, RuneEffectDefinition> runeEffectsById = new(loadedRuneEffects.Count);

        foreach (RuneEffectDefinition runeEffect in loadedRuneEffects)
        {
            if (runeEffectsById.ContainsKey(runeEffect.Id))
            {
                throw new InvalidOperationException("Duplicate rune effect id: " + runeEffect.Id);
            }
            runeEffects.Add(runeEffect);
            runeEffectsById.TryAdd(runeEffect.Id, runeEffect);
        }

        _runeEffects = runeEffects;
        _runeEffectsById = runeEffectsById;

        _databaseReady = true;
        Debug.Log($"Loaded {runeEffects.Count} rune effects.");
    }

    public void ResetInitialization()
    {
        _databaseReady = false;
        _runeEffects?.Clear();
        _runeEffectsById?.Clear();
    }

    public IReadOnlyList<RuneEffectDefinition> GetAll()
    {
        return !_databaseReady ? null : _runeEffects;
    }

    public bool TryGetById(string id, out RuneEffectDefinition runeEffectDefinition)
    {
        runeEffectDefinition = null;
        return _databaseReady && _runeEffectsById.TryGetValue(id, out runeEffectDefinition);
    }

    public bool TryGetByCategory(RuneEffectCategory category, out IReadOnlyList<RuneEffectDefinition> runeEffectData)
    {
        runeEffectData = Array.Empty<RuneEffectDefinition>();
        if (!_databaseReady) return false;
        runeEffectData = _runeEffectsById.Values.Where(data => data.Category == category).ToList();
        return runeEffectData.Count > 0;
    }
}
