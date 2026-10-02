using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SkillDatabase : Singleton<SkillDatabase>
{
    [Header("References")]
    [SerializeField] private SkillDefinitionLoader _loader;

    [Header("Data")]
    [SerializeField] private List<SkillDefinition> _skills = new();
    [SerializeField] private List<SkillNodeDefinition> _skillNodes = new();
    [SerializeField] private List<SkillNodeParameterDefinition> _skillNodeParameters = new();

    private bool _databaseReady;

    private Dictionary<string, SkillDefinition> _skillDatabase;
    private Dictionary<string, SkillNodeDefinition> _skillNodeDatabase;
    private Dictionary<(string NodeId, int Level, ParameterType Parameter), SkillNodeParameterDefinition>
        _skillNodeParameterDatabase;

    public bool DatabaseReady => _databaseReady;

    public IReadOnlyList<SkillDefinition> Skills =>
        _databaseReady ? _skills : Array.Empty<SkillDefinition>();
    public IReadOnlyList<SkillNodeDefinition> SkillNodes =>
        _databaseReady ? _skillNodes : Array.Empty<SkillNodeDefinition>();
    public IReadOnlyList<SkillNodeParameterDefinition> SkillNodeParameters =>
        _databaseReady ? _skillNodeParameters : Array.Empty<SkillNodeParameterDefinition>();

    public async UniTask Initialize()
    {
        IReadOnlyList<SkillDefinition> skills = await _loader.LoadSkillDefinitions();
        IReadOnlyList<SkillNodeDefinition> skillNodes = await _loader.LoadSkillNodeDefinitions();
        IReadOnlyList<SkillNodeParameterDefinition> skillNodeParameters =
            await _loader.LoadSkillNodeParameterDefinitions();

        Dictionary<string, SkillDefinition> skillDatabase = new(skills.Count);
        Dictionary<string, SkillNodeDefinition> skillNodeDatabase = new(skillNodes.Count);
        Dictionary<(string NodeId, int Level, ParameterType Parameter), SkillNodeParameterDefinition>
            skillNodeParameterDatabase = new(skillNodeParameters.Count);

        foreach (SkillDefinition skill in skills)
        {
            if (!skillDatabase.TryAdd(skill.Id, skill))
            {
                throw new InvalidOperationException($"Duplicate skill id: {skill.Id}");
            }
        }

        foreach (SkillNodeDefinition skillNode in skillNodes)
        {
            if (!skillDatabase.ContainsKey(skillNode.SkillId))
            {
                throw new InvalidOperationException($"The skill id: {skillNode.SkillId} does not exist");
            }

            if (!skillNodeDatabase.TryAdd(skillNode.Id, skillNode))
            {
                throw new InvalidOperationException($"Duplicate skill node id: {skillNode.Id}");
            }
        }

        foreach (SkillNodeParameterDefinition skillNodeParameter in skillNodeParameters)
        {
            if (!skillNodeDatabase.ContainsKey(skillNodeParameter.NodeId))
            {
                throw new InvalidOperationException($"The skill node id: {skillNodeParameter.NodeId} does not exist");
            }

            (string NodeId, int Level, ParameterType Parameter) key = (skillNodeParameter.NodeId,
                skillNodeParameter.Level, skillNodeParameter.ParameterType);
            if (!skillNodeParameterDatabase.TryAdd(key, skillNodeParameter))
            {
                throw new InvalidOperationException($"Duplicate skill node parameter key: {key}");
            }
        }

        _skills = new List<SkillDefinition>(skillDatabase.Values);
        _skillNodes = new List<SkillNodeDefinition>(skillNodeDatabase.Values);
        _skillNodeParameters = new List<SkillNodeParameterDefinition>(skillNodeParameterDatabase.Values);

        _skillDatabase = skillDatabase;
        _skillNodeDatabase = skillNodeDatabase;
        _skillNodeParameterDatabase = skillNodeParameterDatabase;
        _databaseReady = true;
        Debug.Log("Skill database initialized.");
    }

    public void ResetInitialization()
    {
        _databaseReady = false;
        _skills?.Clear();
        _skillNodes?.Clear();
        _skillNodeParameters?.Clear();
        _skillDatabase?.Clear();
        _skillNodeDatabase?.Clear();
        _skillNodeParameterDatabase?.Clear();
    }

    public bool TryGetSkillById(string id, out SkillDefinition skillDefinition)
    {
        skillDefinition = null;
        return _databaseReady && _skillDatabase.TryGetValue(id, out skillDefinition);
    }

    public bool TryGetSkillNodeById(string id, out SkillNodeDefinition nodeDefinition)
    {
        nodeDefinition = null;
        return _databaseReady && _skillNodeDatabase.TryGetValue(id, out nodeDefinition);
    }

    public bool TryGetSkillNodesBySkillId(string skillId, out SkillNodeDefinition[] nodeData)
    {
        nodeData = Array.Empty<SkillNodeDefinition>();
        if (!_databaseReady) return false;
        nodeData = _skillNodeDatabase.Values.Where(v => v.SkillId == skillId).ToArray();
        return nodeData.Length != 0;
    }

    public bool TryGetSkillNodeParameterByNodeId(string id, int level, ParameterType parameterType,
        out SkillNodeParameterDefinition nodeParameterDefinition)
    {
        nodeParameterDefinition = null;
        return _databaseReady &&
               _skillNodeParameterDatabase.TryGetValue((id, level, parameterType), out nodeParameterDefinition);
    }
}
