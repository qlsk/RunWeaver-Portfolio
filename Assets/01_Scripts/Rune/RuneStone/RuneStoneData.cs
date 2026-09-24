using UnityEngine;

public class RuneStoneData
{
    private string _id;
    private char _runeCharacter;
    private Sprite _icon;

    public string Id => _id;
    public char RuneCharacter => _runeCharacter;
    public Sprite Icon => _icon;

    public RuneStoneData(string id, char runeCharacter, Sprite icon)
    {
        _id = id;
        _runeCharacter = runeCharacter;
        _icon = icon;
    }
}
