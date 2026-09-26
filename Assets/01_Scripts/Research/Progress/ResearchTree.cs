using System;
using System.Collections.Generic;
using UnityEngine;

public class ResearchTree
{
    private readonly Dictionary<string, ResearchNode> _nodes;
    public event Action<string> NodeUnlocked;
    public event Action<string> NodeActivated;

    public ResearchTree(IReadOnlyList<ResearchNodeDefinition> nodes)
    {
        Dictionary<string, ResearchNode> nodeProgressById = new();
        foreach (ResearchNodeDefinition nodeData in nodes)
        {
            if (string.IsNullOrWhiteSpace(nodeData.Id))
            {
                throw new ArgumentException("Research tree node id cannot be null or empty");
            }
            if (nodeProgressById.ContainsKey(nodeData.Id))
            {
                throw new InvalidOperationException($"Research tree node id '{nodeData.Id}' already exists");
            }
            nodeProgressById[nodeData.Id] = new ResearchNode(nodeData.Id);
        }
        _nodes = nodeProgressById;
        Debug.Log("Research tree progress initialized");
    }

    private ResearchNode CheckValidity(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("Research tree node id cannot be null or empty");
        }
        if (!_nodes.TryGetValue(nodeId, out ResearchNode node))
        {
            throw new InvalidOperationException($"Research tree node id '{nodeId}' does not exist");
        }
        return node;
    }

    public bool GetNodeIsUnlocked(string nodeId)
    {
        return CheckValidity(nodeId).IsUnlocked;
    }

    public bool GetNodeIsActivated(string nodeId)
    {
        return CheckValidity(nodeId).IsActive;
    }

    public void Unlock(string nodeId)
    {
        ResearchNode node = CheckValidity(nodeId);
        if (!node.TryUnlock()) return;
        NodeUnlocked?.Invoke(nodeId);
    }

    public void Activate(string nodeId)
    {
        ResearchNode node = CheckValidity(nodeId);
        if (!node.TryActivate()) return;
        NodeActivated?.Invoke(nodeId);
    }
}
