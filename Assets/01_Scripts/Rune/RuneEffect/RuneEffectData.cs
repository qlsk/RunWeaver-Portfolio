using UnityEngine;

public class RuneEffectData
{
    private string _id;
    private string _runeWord;
    private string _englishName;
    private string _displayName;
    private RuneEffectCategory _category;
    private string _description;
    private Sprite _icon;

    public string Id => _id;
    public string RuneWord => _runeWord;
    public string EnglishName => _englishName;
    public string DisplayName => _displayName;
    public RuneEffectCategory Category => _category;
    public string Description => _description;
    public Sprite Icon => _icon;

    public RuneEffectData(string id, string runeWord, string englishName, string displayName, RuneEffectCategory category,
        string description, Sprite icon)
    {
        _id = id;
        _runeWord = runeWord;
        _englishName = englishName;
        _displayName = displayName;
        _category = category;
        _description = description;
        _icon = icon;
    }
}
