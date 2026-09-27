using System;
using System.Collections.Generic;
using System.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RuneStoneDatabase : Singleton<RuneStoneDatabase>
{
    [Header("References")]
    [SerializeField] private RuneDefinitionLoader _loader;

    [Header("Data")]
    [SerializeField] private List<RuneStoneDefinition> _runeStones;

    private Dictionary<string, RuneStoneDefinition> _runeStonesById;
    private Dictionary<char, RuneStoneDefinition> _runeStonesByRuneCharacter;

    private bool _databaseReady;

    public bool DatabaseReady => _databaseReady;

    public async UniTask Initialize()
    {
        IReadOnlyList<RuneStoneDefinition> loadedRuneStones = await _loader.LoadRuneStoneDefinitions();

        List<RuneStoneDefinition> runeStones = new(loadedRuneStones.Count);
        Dictionary<string, RuneStoneDefinition> runeStonesById = new(loadedRuneStones.Count);
        Dictionary<char, RuneStoneDefinition> runeStonesByRuneCharacter = new(loadedRuneStones.Count);

        foreach (RuneStoneDefinition runeStone in loadedRuneStones)
        {
            if (runeStonesById.ContainsKey(runeStone.Id))
            {
                throw new InvalidOperationException("Duplicate rune stone id: " + runeStone.Id);
            }
            if (runeStonesByRuneCharacter.ContainsKey(runeStone.RuneCharacter))
            {
                throw new InvalidOperationException("Duplicate rune stone character id: " + runeStone.RuneCharacter);
            }
            runeStones.Add(runeStone);
            runeStonesById.TryAdd(runeStone.Id, runeStone);
            runeStonesByRuneCharacter.TryAdd(runeStone.RuneCharacter, runeStone);
        }

        _runeStones = runeStones;
        _runeStonesById = runeStonesById;
        _runeStonesByRuneCharacter = runeStonesByRuneCharacter;

        _databaseReady = true;
        Debug.Log($"Loaded {runeStones.Count} rune stones.");
    }

    public IReadOnlyList<RuneStoneDefinition> GetAll()
    {
        return !_databaseReady ? null : _runeStones;
    }

    public bool TryGetById(string id, out RuneStoneDefinition runeStoneDefinition)
    {
        return _runeStonesById.TryGetValue(id, out runeStoneDefinition);
    }

    public bool TryGetByRuneCharacter(char runeCharacter, out RuneStoneDefinition runeStoneDefinition)
    {
        return _runeStonesByRuneCharacter.TryGetValue(runeCharacter, out runeStoneDefinition);
    }
}
