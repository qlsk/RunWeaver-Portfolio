using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ResearchDatabase : Singleton<ResearchDatabase>
{
    [Header("References")]
    [SerializeField] private ResearchDefinitionLoader _loader;

    [Header("Data")]
    [SerializeField] private List<ResearchNodeDefinition> _researchNodes = new();

    private Dictionary<string, ResearchNodeDefinition> _researchNodesById;
    private bool _databaseReady;

    public bool DatabaseReady => _databaseReady;
    public IReadOnlyList<ResearchNodeDefinition> ResearchNodes =>
        _databaseReady ? _researchNodes : Array.Empty<ResearchNodeDefinition>();

    public async UniTask Initialize()
    {
        IReadOnlyList<ResearchNodeDefinition> nodes = await _loader.LoadResearchNodeDefinitions();
        Dictionary<string, ResearchNodeDefinition> nodesById = new(nodes.Count);
        foreach (ResearchNodeDefinition node in nodes)
        {
            if (!nodesById.TryAdd(node.Id, node))
                throw new InvalidOperationException($"Duplicate research node id: {node.Id}");
        }

        foreach (ResearchNodeDefinition node in nodes)
        {
            if (node.NodeType == ResearchNodeType.Root)
            {
                if (!string.IsNullOrWhiteSpace(node.RootId))
                    throw new InvalidOperationException($"Research root cannot have a root id: {node.Id}");
                continue;
            }
            if (string.IsNullOrWhiteSpace(node.RootId) ||
                !nodesById.TryGetValue(node.RootId, out ResearchNodeDefinition root) ||
                root.NodeType != ResearchNodeType.Root || root.Category != node.Category)
            {
                throw new InvalidOperationException($"Invalid research root: {node.Id}, {node.RootId}");
            }
        }

        HashSet<string> visiting = new();
        HashSet<string> visited = new();
        foreach (ResearchNodeDefinition node in nodes)
            ValidatePrerequisites(node, nodesById, visiting, visited);

        _researchNodes = new List<ResearchNodeDefinition>(nodes);
        _researchNodesById = nodesById;
        _databaseReady = true;
        Debug.Log("Research database initialized.");
    }

    private static void ValidatePrerequisites(ResearchNodeDefinition node,
        Dictionary<string, ResearchNodeDefinition> nodesById, HashSet<string> visiting, HashSet<string> visited)
    {
        if (visited.Contains(node.Id)) return;
        if (!visiting.Add(node.Id))
            throw new InvalidOperationException($"Research prerequisite cycle: {node.Id}");
        foreach (string prerequisiteId in node.PrerequisiteIds)
        {
            if (!nodesById.TryGetValue(prerequisiteId, out ResearchNodeDefinition prerequisite))
                throw new InvalidOperationException($"Research prerequisite does not exist: {prerequisiteId}");
            ValidatePrerequisites(prerequisite, nodesById, visiting, visited);
        }
        visiting.Remove(node.Id);
        visited.Add(node.Id);
    }

    public void ResetInitialization()
    {
        _databaseReady = false;
        _researchNodes?.Clear();
        _researchNodesById?.Clear();
    }

    public bool TryGetResearchNodeById(string id, out ResearchNodeDefinition nodeDefinition)
    {
        nodeDefinition = null;
        return _databaseReady && _researchNodesById.TryGetValue(id, out nodeDefinition);
    }

    public bool TryGetResearchNodesByRootId(string rootId, out ResearchNodeDefinition[] nodes)
    {
        nodes = Array.Empty<ResearchNodeDefinition>();
        if (!_databaseReady) return false;
        nodes = _researchNodesById.Values.Where(node => node.RootId == rootId).ToArray();
        return nodes.Length != 0;
    }
}
