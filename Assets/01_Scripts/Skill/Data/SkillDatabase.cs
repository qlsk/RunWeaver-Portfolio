using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillDatabase : Singleton<SkillDatabase>
{
    private bool _isInitialized = false;

    private Dictionary<string, SkillData> _skillDatabase = new();
    private Dictionary<string, SkillNodeData> _skillNodeDatabase = new();
    private Dictionary<(string NodeId, int Level, ParameterType Parameter), SkillNodeParameterData>
        _skillNodeParameterDatabase = new();

    private IReadOnlyList<SkillData> _skills = new List<SkillData>();
    private IReadOnlyList<SkillNodeData> _skillNodes = new List<SkillNodeData>();
    private IReadOnlyList<SkillNodeParameterData> _skillNodeParameters = new List<SkillNodeParameterData>();

    public void Initialize(IReadOnlyList<SkillData> skills, IReadOnlyList<SkillNodeData> skillNodes,
        IReadOnlyList<SkillNodeParameterData> skillNodeParameters)
    {
        if (_isInitialized) return;

        _skillDatabase.Clear();
        _skillNodeDatabase.Clear();
        _skillNodeParameterDatabase.Clear();

        foreach (SkillData skill in skills)
        {
            if (!_skillDatabase.TryAdd(skill.Id, skill))
            {
                Debug.LogError($"Duplicate skill id: {skill.Id}");
            }
        }

        foreach (SkillNodeData skillNode in skillNodes)
        {
            if (!_skillDatabase.ContainsKey(skillNode.SkillId))
            {
                Debug.LogError($"The skill id: {skillNode.SkillId} does not exist");
                continue;
            }

            if (!_skillNodeDatabase.TryAdd(skillNode.Id, skillNode))
            {
                Debug.LogError($"Duplicate skill node id: {skillNode.Id}");
            }
        }

        foreach (SkillNodeParameterData skillNodeParameter in skillNodeParameters)
        {
            if (!_skillNodeDatabase.ContainsKey(skillNodeParameter.NodeId))
            {
                Debug.LogError($"The skill node id: {skillNodeParameter.NodeId} does not exist");
                continue;
            }

            (string NodeId, int Level, ParameterType Parameter) key = (skillNodeParameter.NodeId,
                skillNodeParameter.Level, skillNodeParameter.ParameterType);
            if (!_skillNodeParameterDatabase.TryAdd(key, skillNodeParameter))
            {
                Debug.LogError($"Duplicate skill node parameter key: {key}");
            }
        }

        _skills = new List<SkillData>(_skillDatabase.Values);
        _skillNodes = new List<SkillNodeData>(_skillNodeDatabase.Values);
        _skillNodeParameters = new List<SkillNodeParameterData>(_skillNodeParameterDatabase.Values);

        _isInitialized = true;
        Debug.Log($"SkillDatabase initialization complete");
    }

    public bool TryGetSkillById(string id, out SkillData skillData)
    {
        return _skillDatabase.TryGetValue(id, out skillData);
    }

    public bool TryGetSkillNodeById(string id, out SkillNodeData nodeData)
    {
        return _skillNodeDatabase.TryGetValue(id, out nodeData);
    }

    public bool TryGetSkillNodesBySkillId(string skillId, out SkillNodeData[] nodeData)
    {
        nodeData = _skillNodeDatabase.Values.Where(v => v.SkillId == skillId).ToArray();
        return nodeData.Length != 0;
    }

    public bool TryGetSkillNodeParameterByNodeId(string id, int level, ParameterType parameterType,
        out SkillNodeParameterData nodeParameterData)
    {
        return _skillNodeParameterDatabase.TryGetValue((id, level, parameterType), out nodeParameterData);
    }
}
