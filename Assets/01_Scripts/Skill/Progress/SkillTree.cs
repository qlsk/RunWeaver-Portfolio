using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillTree
{
    private readonly Dictionary<string, SkillNode> _nodes;

    public event Action<string, int> NodeLevelChanged;

    public SkillTree(IReadOnlyList<SkillNodeDefinition> nodes)
    {
        Dictionary<string, SkillNode> nodeProgressById = new();
        foreach (SkillNodeDefinition node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                throw new ArgumentException("Skill tree node id cannot be null or empty");
            }
            if (nodeProgressById.ContainsKey(node.Id))
            {
                throw new InvalidOperationException($"Skill tree node id '{node.Id}' already exists");
            }
            nodeProgressById[node.Id] = new SkillNode(node.Id, node.MaxLevel);
        }
        _nodes = nodeProgressById;
        Debug.Log("Skill tree progress initialized");
    }

    private SkillNode CheckValidity(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("Skill tree node id cannot be null or empty");
        }
        if (!_nodes.TryGetValue(nodeId, out SkillNode node))
        {
            throw new InvalidOperationException($"Skill tree node id '{nodeId}' does not exist");
        }
        return node;
    }

    public int GetNodeLevel(string nodeId)
    {
        return CheckValidity(nodeId).Level;
    }

    public void IncreaseLevel(string nodeId)
    {
        SkillNode node = CheckValidity(nodeId);
        if (!node.TryIncreaseLevel()) return;
        NodeLevelChanged?.Invoke(nodeId, node.Level);
    }
}
