using UnityEngine;

public class SkillData
{
    private string _id;
    private string _displayName;
    private string _description;
    private Sprite _icon;

    public string Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;

    public SkillData(string id, string displayName, string description, Sprite icon)
    {
        _id = id;
        _displayName = displayName;
        _description = description;
        _icon = icon;
    }
}
