using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResearchNodeDefinition
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private ResearchCategory _category;
    [SerializeField] private string _description;
    [SerializeField] private ResearchNodeType _nodeType;
    [SerializeField] private string _rootId;
    [SerializeField] private List<string> _prerequisiteIds;
    [SerializeField] private List<ResearchCost> _costs;
    [SerializeField] private List<ResearchEffect> _effects;

    public string Id => _id;
    public string DisplayName => _displayName;
    public ResearchCategory Category => _category;
    public string Description => _description;
    public ResearchNodeType NodeType => _nodeType;
    public string RootId => _rootId;
    public List<string> PrerequisiteIds => _prerequisiteIds;
    public List<ResearchCost> Costs => _costs;
    public List<ResearchEffect> Effects => _effects;

    public ResearchNodeDefinition(
        string id,
        string displayName,
        ResearchCategory category,
        string description,
        ResearchNodeType nodeType,
        string rootId,
        List<string> prerequisiteIds,
        List<ResearchCost> costs,
        List<ResearchEffect> effects)
    {
        _id = id;
        _displayName = displayName;
        _category = category;
        _description = description;
        _nodeType = nodeType;
        _rootId = rootId;
        _prerequisiteIds = prerequisiteIds;
        _costs = costs;
        _effects = effects;
    }
}
