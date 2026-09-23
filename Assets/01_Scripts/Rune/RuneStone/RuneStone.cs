using UnityEngine;

public class RuneStone
{
    private string _id;
    private char _runeCharacter;
    private Sprite _runeStoneSprite;

    public string Id => _id;
    public char RuneCharacter => _runeCharacter;
    public Sprite RuneStoneSprite => _runeStoneSprite;

    public RuneStone(string id, char runeCharacter, Sprite runeStoneSprite)
    {
        _id = id;
        _runeCharacter = runeCharacter;
        _runeStoneSprite = runeStoneSprite;
    }
}
