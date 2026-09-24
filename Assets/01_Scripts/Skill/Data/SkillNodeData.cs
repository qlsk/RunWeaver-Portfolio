public class SkillNodeData
{
    private string _id;
    private string _displayName;
    private string _description;
    private string _skillId;
    private string _prerequisiteNodeId;
    private int _maxLevel;
    private int _upgradeCost;

    public string Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;
    public string SkillId => _skillId;
    public string PrerequisiteNodeId => _prerequisiteNodeId;
    public int MaxLevel => _maxLevel;
    public int UpgradeCost => _upgradeCost;

    public SkillNodeData(string id, string displayName, string description, string skillId,
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
