using UnityEngine;

[System.Serializable]
public class RuneEffectDefinition
{
    [SerializeField] private string _id;
    [SerializeField] private string _runeWord;
    [SerializeField] private string _englishName;
    [SerializeField] private string _displayName;
    [SerializeField] private RuneEffectCategory _category;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;

    public string Id => _id;
    public string RuneWord => _runeWord;
    public string EnglishName => _englishName;
    public string DisplayName => _displayName;
    public RuneEffectCategory Category => _category;
    public string Description => _description;
    public Sprite Icon => _icon;

    public RuneEffectDefinition(string id, string runeWord, string englishName, string displayName,
        RuneEffectCategory category, string description, Sprite icon)
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
