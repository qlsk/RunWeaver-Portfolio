using UnityEngine;

public class ResearchNodeDefinition
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;
    [SerializeField] private string _skillId;
    [SerializeField] private string _prerequisiteNodeId;
    [SerializeField] private int _maxLevel;
    [SerializeField] private int _upgradeCost;

    public string Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;
    public string SkillId => _skillId;
    public string PrerequisiteNodeId => _prerequisiteNodeId;
    public int MaxLevel => _maxLevel;
    public int UpgradeCost => _upgradeCost;

    public ResearchNodeDefinition(string id, string displayName, string description, string skillId,
        string prerequisiteNodeId, int maxLevel, int upgradeCost)
    {
        _id = id;
        _displayName = displayName;
        _description = description;
        _skillId = skillId;
        _prerequisiteNodeId = prerequisiteNodeId;
        _maxLevel = maxLevel;
        _upgradeCost = upgradeCost;
    }
}
