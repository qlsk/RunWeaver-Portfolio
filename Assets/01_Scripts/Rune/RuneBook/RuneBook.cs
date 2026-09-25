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
            throw new ArgumentNullException(nameof(database), "룬 도감 생성 실패: 전달받은 룬 효과 데이터베이스가 없거나 이미 파괴되었습니다.");
        }
        _database = database;
        _isInitialized = false;
    }

    public bool TryInitialize()
    {
        if (_isInitialized)
        {
            Debug.LogError("룬 도감 초기화 실패: 이미 초기화되어 있습니다.");
            return false;
        }
        if (_database == null)
        {
            Debug.LogError("룬 도감 초기화 실패: 룬 효과 데이터베이스 인스턴스가 없습니다.");
            return false;
        }
        if (!_database.IsInitialized)
        {
            Debug.LogError("룬 도감 초기화 실패: 룬 효과 데이터베이스가 아직 초기화되지 않았습니다.");
            return false;
        }
        Dictionary<string, RuneWord> runeWords = new();
        foreach (RuneEffectData data in _database.GetAll())
        {
            RuneWord runeWord = new(data.Id);
            runeWords[data.Id] = runeWord;
        }
        _runeWords = runeWords;
        _isInitialized = true;
        RuneBookChanged?.Invoke();
        return true;
    }

    public bool RevealCategory(RuneEffectCategory category)
    {
        if (!_isInitialized)
        {
            Debug.LogError($"'{category}' 카테고리 공개 실패: 룬 도감이 아직 초기화되지 않았습니다.");
            return false;
        }
        if (!_database.TryGetByCategory(category, out IReadOnlyList<RuneEffectData> runeEffectData))
        {
            Debug.LogError(
                $"'{category}' 카테고리 공개 실패: 데이터베이스에 해당 카테고리의 룬 효과가 없습니다.");
            return false;
        }
        foreach (RuneEffectData data in runeEffectData)
        {
            if (!_runeWords.TryGetValue(data.Id, out RuneWord runeWord))
            {
                Debug.LogError(
                    $"'{category}' 카테고리 공개 실패: 룬 도감에 룬 효과 '{data.Id}'의 항목이 없습니다.");
                return false;
            }
            runeWord.TryReveal(RuneWordRevealLevel.Listed);
        }
        RuneBookChanged?.Invoke();
        return true;
    }

    public bool RevealDetails(string runeEffectId)
    {
        if (!_isInitialized)
        {
            Debug.LogError(
                $"룬 효과 '{runeEffectId}'의 상세 정보 공개 실패: 룬 도감이 아직 초기화되지 않았습니다.");
            return false;
        }
        if (!_runeWords.TryGetValue(runeEffectId, out RuneWord runeWord))
        {
            Debug.LogError(
                $"룬 효과 '{runeEffectId}'의 상세 정보 공개 실패: 룬 도감에 해당 항목이 없습니다.");
            return false;
        }
        if (!runeWord.TryReveal(RuneWordRevealLevel.Detailed))
        {
            Debug.LogError(
                $"룬 효과 '{runeEffectId}'의 상세 정보 공개 실패: 현재 공개 단계 '{runeWord.RevealLevel}'에서 상세 공개 단계로 변경할 수 없습니다.");
            return false;
        }
        RuneBookChanged?.Invoke();
        return true;
    }

    public bool IsRevealed(string runeEffectId)
    {
        if (!_isInitialized)
        {
            Debug.LogError(
                $"룬 효과 '{runeEffectId}'의 공개 여부 확인 실패: 룬 도감이 아직 초기화되지 않았습니다.");
            return false;
        }
        if (!_runeWords.TryGetValue(runeEffectId, out RuneWord runeWord))
        {
            Debug.LogError(
                $"룬 효과 '{runeEffectId}'의 공개 여부 확인 실패: 룬 도감에 해당 항목이 없습니다.");
            return false;
        }
        return runeWord.IsRevealed;
    }

    public bool TryGetWord(string runeEffectId, out RuneWordInfo info)
    {
        info = null;
        if (!_isInitialized)
        {
            Debug.LogError(
                $"룬 도감 항목 '{runeEffectId}' 조회 실패: 룬 도감이 아직 초기화되지 않았습니다.");
            return false;
        }
        if (!_database.TryGetById(runeEffectId, out RuneEffectData data))
        {
            Debug.LogError(
                $"룬 도감 항목 '{runeEffectId}' 조회 실패: 데이터베이스에 해당 룬 효과가 없습니다.");
            return false;
        }
        if (!_runeWords.TryGetValue(runeEffectId, out RuneWord runeWord))
        {
            Debug.LogError(
                $"룬 도감 항목 '{runeEffectId}' 조회 실패: 도감에 해당 항목의 공개 상태가 없습니다.");
            return false;
        }
        info = new RuneWordInfo(data, runeWord.RevealLevel);
        return true;
    }

    public IReadOnlyList<RuneWordInfo> GetAll()
    {
        if (!_isInitialized) return Array.Empty<RuneWordInfo>();
        List<RuneWordInfo> infos = new();
        foreach (RuneWord word in _runeWords.Values)
        {
            if (!_database.TryGetById(word.RuneEffectId, out RuneEffectData data))
            {
                Debug.LogError(
                    $"룬 도감 전체 목록 조회 실패: 데이터베이스에 룬 효과 '{word.RuneEffectId}'가 없습니다.");
                return Array.Empty<RuneWordInfo>();
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
        foreach (RuneWord word in _runeWords.Values)
        {
            if (!word.IsRevealed) continue;
            if (!_database.TryGetById(word.RuneEffectId, out RuneEffectData data))
            {
                Debug.LogError(
                    $"공개된 룬 도감 목록 조회 실패: 데이터베이스에 룬 효과 '{word.RuneEffectId}'가 없습니다.");
                return Array.Empty<RuneWordInfo>();
            }
            RuneWordInfo info = new(data, word.RevealLevel);
            infos.Add(info);
        }
        return infos;
    }
}
