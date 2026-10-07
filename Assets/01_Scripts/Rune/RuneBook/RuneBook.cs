using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룬 워드 공개 상태(도감). 모든 룬 워드는 처음부터 Listed이며, 장착하면 RevealDetails로 Detailed가 된다.
/// (연구로 카테고리를 해금하는 Hidden 단계는 폐기)
/// </summary>
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
        if (runeWord.RevealLevel == RuneWordRevealLevel.Detailed)
        {
            return;
        }
        runeWord.Reveal(RuneWordRevealLevel.Detailed);
        SafeEventNotifier.Notify(RuneBookChanged);
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
}
