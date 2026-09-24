using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillTreeProgress
{
    private Dictionary<string, SkillNodeProgress> _nodeProgressById;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public void Initialize(IReadOnlyList<SkillNodeData> nodes)
    {
        if (_isInitialized) return;
        Dictionary<string, SkillNodeProgress> nodeProgressById = new();
        foreach (SkillNodeData nodeData in nodes)
        {
            if (string.IsNullOrWhiteSpace(nodeData.Id))
            {
                Debug.LogError("Skill node id is empty");
                continue;
            }
            if (nodeProgressById.ContainsKey(nodeData.Id))
            {
                Debug.LogError($"Duplicate node id: {nodeData.Id}");
                continue;
            }
            nodeProgressById[nodeData.Id] = new SkillNodeProgress(nodeData.Id);
        }
        _nodeProgressById = nodeProgressById;
        _isInitialized = true;
        Debug.Log("Skill tree progress initialized");
    }

    public bool TryGetNodeProgress(string nodeId, out SkillNodeProgress progress)
    {
        if (!_isInitialized) throw new InvalidOperationException("Skill tree is not initialized");
        return _nodeProgressById.TryGetValue(nodeId, out progress);
    }
}
