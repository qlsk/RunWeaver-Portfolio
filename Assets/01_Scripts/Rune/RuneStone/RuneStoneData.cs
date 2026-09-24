using UnityEngine;

public class RuneStoneData
{
    private string _id;
    private char _runeCharacter;
    private Sprite _runeStoneSprite;

    public string Id => _id;
    public char RuneCharacter => _runeCharacter;
    public Sprite RuneStoneSprite => _runeStoneSprite;

    public RuneStoneData(string id, char runeCharacter, Sprite runeStoneSprite)
    {
        _id = id;
        _runeCharacter = runeCharacter;
        _runeStoneSprite = runeStoneSprite;
    }
}
