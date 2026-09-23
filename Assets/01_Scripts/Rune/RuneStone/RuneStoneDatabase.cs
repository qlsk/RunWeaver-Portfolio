using System.Collections.Generic;
using UnityEngine;

public class RuneStoneDatabase : Singleton<RuneStoneDatabase>
{
    private bool _isInitialized = false;
    private Dictionary<string, RuneStone> _databaseById = new();
    private Dictionary<char, RuneStone> _databaseByRuneCharacter = new();
    private IReadOnlyList<RuneStone> _runeStones = new List<RuneStone>();

    public void Initialize(IReadOnlyList<RuneStone> stones)
    {
        if (_isInitialized) return;
        _databaseById.Clear();
        _databaseByRuneCharacter.Clear();
        foreach (RuneStone stone in stones)
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

        _runeStones = new List<RuneStone>(_databaseById.Values);
        _isInitialized = true;
        Debug.Log($"룬석 데이터베이스를 초기화했습니다.");
    }

    public IReadOnlyList<RuneStone> GetAll()
    {
        return _runeStones;
    }

    public bool TryGetById(string id, out RuneStone runeStone)
    {
        return _databaseById.TryGetValue(id, out runeStone);
    }

    public bool TryGetByRuneCharacter(char runeCharacter, out RuneStone runeStone)
    {
        return _databaseByRuneCharacter.TryGetValue(runeCharacter, out runeStone);
    }
}
