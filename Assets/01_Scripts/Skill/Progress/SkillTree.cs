using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillTree
{
    private readonly Dictionary<string, SkillNode> _nodes;
    private readonly Dictionary<string, SkillNodeDefinition> _definitions;

    public event Action<string, int> NodeLevelChanged;

    public SkillTree(IReadOnlyList<SkillNodeDefinition> nodes)
    {
        Dictionary<string, SkillNode> nodeProgressById = new();
        Dictionary<string, SkillNodeDefinition> definitionById = new();
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
            definitionById[node.Id] = node;
        }
        _nodes = nodeProgressById;
        _definitions = definitionById;
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

    public bool ContainsNode(string nodeId)
    {
        return !string.IsNullOrWhiteSpace(nodeId) && _nodes.ContainsKey(nodeId);
    }

    public int GetNodeLevel(string nodeId)
    {
        return CheckValidity(nodeId).Level;
    }

    public int GetMaxLevel(string nodeId)
    {
        CheckValidity(nodeId);
        return _definitions[nodeId].MaxLevel;
    }

    public bool IsMaxLevel(string nodeId)
    {
        return CheckValidity(nodeId).IsCompleted;
    }

    /// <summary>
    /// 다음 레벨로 올릴 때 필요한 재료 수량
    /// </summary>
    public int GetUpgradeCost(string nodeId)
    {
        CheckValidity(nodeId);
        return _definitions[nodeId].UpgradeCost;
    }

    /// <summary>
    /// 선행 노드 id. 선행 노드가 없으면 빈 문자열을 반환한다.
    /// </summary>
    public string GetPrerequisiteNodeId(string nodeId)
    {
        CheckValidity(nodeId);
        return _definitions[nodeId].PrerequisiteNodeId ?? string.Empty;
    }

    /// <summary>
    /// 선행 노드가 없거나, 선행 노드가 1레벨 이상이면 true
    /// </summary>
    public bool IsPrerequisiteMet(string nodeId)
    {
        string prerequisiteNodeId = GetPrerequisiteNodeId(nodeId);
        if (string.IsNullOrWhiteSpace(prerequisiteNodeId)) return true;
        if (!_nodes.TryGetValue(prerequisiteNodeId, out SkillNode prerequisite))
        {
            Debug.LogWarning($"Prerequisite node '{prerequisiteNodeId}' of '{nodeId}' does not exist");
            return false;
        }
        return prerequisite.IsActivated;
    }

    /// <summary>
    /// 재료를 제외한 레벨 업 조건(최대 레벨 미만, 선행 노드 충족)을 만족하면 true
    /// </summary>
    public bool CanIncreaseLevel(string nodeId)
    {
        return !IsMaxLevel(nodeId) && IsPrerequisiteMet(nodeId);
    }

    /// <summary>
    /// 레벨 업 조건과 재료 보유량을 모두 만족하면 true
    /// </summary>
    public bool CanUpgrade(string nodeId, ResourceInventory inventory, string resourceId)
    {
        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }
        if (!CanIncreaseLevel(nodeId)) return false;
        return inventory.GetQuantity(resourceId) >= GetUpgradeCost(nodeId);
    }

    /// <summary>
    /// 조건을 확인한 뒤 재료를 차감하고 레벨을 1 올린다.
    /// </summary>
    public bool TryUpgrade(string nodeId, ResourceInventory inventory, string resourceId)
    {
        if (!CanUpgrade(nodeId, inventory, resourceId)) return false;

        List<ResourceCost> costs = new() { new ResourceCost(resourceId, GetUpgradeCost(nodeId)) };
        if (!inventory.TrySpend(costs)) return false;

        IncreaseLevel(nodeId);
        return true;
    }

    public void IncreaseLevel(string nodeId)
    {
        SkillNode node = CheckValidity(nodeId);
        if (!node.TryIncreaseLevel()) return;
        NodeLevelChanged?.Invoke(nodeId, node.Level);
    }
}
