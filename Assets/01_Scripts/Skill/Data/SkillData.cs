using UnityEngine;

[System.Serializable]
public class SkillData
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;

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
