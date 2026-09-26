using System.Collections.Generic;
using UnityEngine;

public class RuneStoneDatabase : Singleton<RuneStoneDatabase>
{
    private bool _isInitialized = false;
    private Dictionary<string, RuneStoneData> _databaseById = new();
    private Dictionary<char, RuneStoneData> _databaseByRuneCharacter = new();
    [SerializeField] private List<RuneStoneData> _runeStones = new();

    public void Initialize(IReadOnlyList<RuneStoneData> stones)
    {
        if (_isInitialized) return;
        _databaseById.Clear();
        _databaseByRuneCharacter.Clear();
        _runeStones.Clear();
        foreach (RuneStoneData stone in stones)
        {
            if (_databaseById.ContainsKey(stone.Id))
            {
                Debug.LogError($"중복된 룬 아이디입니다: {stone.Id}");
                continue;
            }

            if (_databaseByRuneCharacter.ContainsKey(stone.RuneCharacter))
            {
                Debug.LogError($"중복된 룬 문자입니다: {stone.RuneCharacter}");
                continue;
            }

            _databaseById[stone.Id] = stone;
            _databaseByRuneCharacter[stone.RuneCharacter] = stone;
        }

        _runeStones = new List<RuneStoneData>(_databaseById.Values);
        _isInitialized = true;
        Debug.Log($"룬석 데이터베이스를 초기화했습니다.");
    }

    public IReadOnlyList<RuneStoneData> GetAll()
    {
        return _runeStones;
    }

    public bool TryGetById(string id, out RuneStoneData runeStoneData)
    {
        return _databaseById.TryGetValue(id, out runeStoneData);
    }

    public bool TryGetByRuneCharacter(char runeCharacter, out RuneStoneData runeStoneData)
    {
        return _databaseByRuneCharacter.TryGetValue(runeCharacter, out runeStoneData);
    }
}
