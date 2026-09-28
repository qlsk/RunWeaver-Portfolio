using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RuneBook
{
    private RuneEffectDatabase _database;
    private Dictionary<string, RuneWord> _runeWords;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;
    public event Action RuneBookChanged;

    public RuneBook(RuneEffectDatabase database)
    {
        if (database == null)
        {
            throw new ArgumentNullException(nameof(database), "Rune effect database is missing or destroyed.");
        }
        _database = database;
        _isInitialized = false;
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("Rune book is already initialized.");
        }
        if (_database == null)
        {
            throw new InvalidOperationException("Rune effect database is missing.");
        }
        if (!_database.DatabaseReady)
        {
            throw new InvalidOperationException("Rune effect database is not initialized.");
        }
        Dictionary<string, RuneWord> runeWords = new();
        foreach (RuneEffectDefinition data in _database.GetAll())
        {
            RuneWord runeWord = new(data.Id);
            runeWords[data.Id] = runeWord;
        }
        _runeWords = runeWords;
        _isInitialized = true;
        RuneBookChanged?.Invoke();
    }

    public void RevealCategory(RuneEffectCategory category)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Rune book is not initialized.");
        }
        if (!_database.TryGetByCategory(category, out IReadOnlyList<RuneEffectDefinition> runeEffectData))
        {
            throw new ArgumentException($"No rune effects in category: {category}", nameof(category));
        }
        foreach (RuneEffectDefinition data in runeEffectData)
        {
            if (!_runeWords.TryGetValue(data.Id, out RuneWord runeWord))
            {
                throw new InvalidOperationException($"Rune book entry not found: {data.Id}");
            }
            if (runeWord.IsRevealed) continue;
            runeWord.Reveal(RuneWordRevealLevel.Listed);
        }
        RuneBookChanged?.Invoke();
    }

    public void RevealDetails(string runeEffectId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Rune book is not initialized.");
        }
        if (!_runeWords.TryGetValue(runeEffectId, out RuneWord runeWord))
        {
            throw new ArgumentException($"Rune book entry not found: {runeEffectId}",
                nameof(runeEffectId));
        }
        runeWord.Reveal(RuneWordRevealLevel.Detailed);
        RuneBookChanged?.Invoke();
    }

    public bool IsRevealed(string runeEffectId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Rune book is not initialized.");
        }
        if (!_runeWords.TryGetValue(runeEffectId, out RuneWord runeWord))
        {
            throw new ArgumentException($"Rune book entry not found: {runeEffectId}",
                nameof(runeEffectId));
        }
        return runeWord.IsRevealed;
    }

    public RuneWordInfo GetWord(string runeEffectId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Rune book is not initialized.");
        }
        if (!_database.TryGetById(runeEffectId, out RuneEffectDefinition data))
        {
            throw new ArgumentException($"Rune effect not found in database: {runeEffectId}",
                nameof(runeEffectId));
        }
        if (!_runeWords.TryGetValue(runeEffectId, out RuneWord runeWord))
        {
            throw new InvalidOperationException($"Rune reveal state not found: {runeEffectId}");
        }
        return new RuneWordInfo(data, runeWord.RevealLevel);
    }

    public IReadOnlyList<RuneWordInfo> GetAll()
    {
        if (!_isInitialized) return Array.Empty<RuneWordInfo>();
        List<RuneWordInfo> infos = new();
        foreach (RuneWord word in _runeWords.Values)
        {
            if (!_database.TryGetById(word.RuneEffectId, out RuneEffectDefinition data))
            {
                throw new InvalidOperationException($"Rune effect not found in database: {word.RuneEffectId}");
            }
            RuneWordInfo info = new(data, word.RevealLevel);
            infos.Add(info);
        }
        return infos;
    }

    public IReadOnlyList<RuneWordInfo> GetRevealedEntries()
    {
        if (!_isInitialized) return Array.Empty<RuneWordInfo>();
        List<RuneWordInfo> infos = new();
        foreach (RuneWord word in _runeWords.Values.Where(word => word.IsRevealed))
        {
            if (!_database.TryGetById(word.RuneEffectId, out RuneEffectDefinition data))
            {
                throw new InvalidOperationException($"Rune effect not found in database: {word.RuneEffectId}");
            }
            RuneWordInfo info = new(data, word.RevealLevel);
            infos.Add(info);
        }
        return infos;
    }
}
