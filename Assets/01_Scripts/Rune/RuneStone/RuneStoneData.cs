using UnityEngine;

[System.Serializable]
public class RuneStoneData
{
    [SerializeField] private string _id;
    [SerializeField] private char _runeCharacter;
    [SerializeField] private Sprite _icon;

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
