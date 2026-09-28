using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ResearchDefinitionLoader : MonoBehaviour
{
    private const string PublishedCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vQ9n699TL7eORpnIbC_wYbd1FIu4-ABJno6HSoSjbqYTTYAdPFtfjUATgfmv-IxAnyIGLKqzt-Pd-uR/pub?single=true&output=csv&gid=";

    [Header("CSV URL")]
    [SerializeField] private string _researchNodeCsvUrl = PublishedCsvUrl + "584732176";
    [SerializeField] private string _researchPrerequisiteCsvUrl = PublishedCsvUrl + "1157822266";
    [SerializeField] private string _researchCostCsvUrl = PublishedCsvUrl + "218911871";
    [SerializeField] private string _researchEffectCsvUrl = PublishedCsvUrl + "1216300861";

    public async UniTask<IReadOnlyList<ResearchNodeDefinition>> LoadResearchNodeDefinitions()
    {
        IReadOnlyList<ResearchNodeCsvHeader> nodes =
            await LoadRows<ResearchNodeCsvHeader>(_researchNodeCsvUrl);
        IReadOnlyList<ResearchPrerequisiteCsvHeader> prerequisites =
            await LoadRows<ResearchPrerequisiteCsvHeader>(_researchPrerequisiteCsvUrl);
        IReadOnlyList<ResearchCostCsvHeader> costs =
            await LoadRows<ResearchCostCsvHeader>(_researchCostCsvUrl);
        IReadOnlyList<ResearchEffectCsvHeader> effects =
            await LoadRows<ResearchEffectCsvHeader>(_researchEffectCsvUrl);
        return CreateResearchNodeList(nodes, prerequisites, costs, effects);
    }

    private static async UniTask<IReadOnlyList<T>> LoadRows<T>(string url)
    {
        string csv = await CsvLoader.DownloadCsv(url);
        return string.IsNullOrWhiteSpace(csv)
            ? throw new InvalidDataException($"{typeof(T).Name} CSV is empty.")
            : CsvLoader.ParseCsv<T>(csv);
    }

    private static IReadOnlyList<ResearchNodeDefinition> CreateResearchNodeList(
        IReadOnlyList<ResearchNodeCsvHeader> nodeRows,
        IReadOnlyList<ResearchPrerequisiteCsvHeader> prerequisiteRows,
        IReadOnlyList<ResearchCostCsvHeader> costRows,
        IReadOnlyList<ResearchEffectCsvHeader> effectRows)
    {
        List<ResearchNodeDefinition> nodes = new(nodeRows.Count);
        Dictionary<string, ResearchNodeDefinition> nodesById = new(nodeRows.Count);
        foreach (ResearchNodeCsvHeader row in nodeRows)
        {
            if (string.IsNullOrWhiteSpace(row.Id))
            {
                throw new InvalidDataException("Research node id is empty.");
            }
            ResearchCategory category = ParseEnum<ResearchCategory>(row.Category, row.Id);
            ResearchNodeType nodeType = ParseEnum<ResearchNodeType>(row.NodeType, row.Id);
            ResearchNodeDefinition node = new(row.Id, row.DisplayName, category, row.Description, nodeType, row.RootId,
                new List<string>(), new List<ResearchCost>(), new List<ResearchEffect>());
            if (!nodesById.TryAdd(row.Id, node))
            {
                throw new InvalidDataException($"Duplicate research node id: {row.Id}");
            }
            nodes.Add(node);
        }

        foreach (ResearchPrerequisiteCsvHeader row in prerequisiteRows)
        {
            ResearchNodeDefinition node = GetResearchNode(nodesById, row.ResearchId);
            GetResearchNode(nodesById, row.PrerequisiteResearchId);
            if (row.ResearchId == row.PrerequisiteResearchId ||
                node.PrerequisiteIds.Contains(row.PrerequisiteResearchId))
            {
                throw new InvalidDataException(
                    $"Invalid research prerequisite: {row.ResearchId}, {row.PrerequisiteResearchId}");
            }
            node.PrerequisiteIds.Add(row.PrerequisiteResearchId);
        }

        HashSet<(string ResearchId, string ResourceId)> costKeys = new();
        foreach (ResearchCostCsvHeader row in costRows)
        {
            ResearchNodeDefinition node = GetResearchNode(nodesById, row.ResearchId);
            if (string.IsNullOrWhiteSpace(row.ResourceId) || row.Amount <= 0)
            {
                throw new InvalidDataException($"Invalid research cost: {row.ResearchId}, {row.ResourceId}");
            }
            if (!costKeys.Add((row.ResearchId, row.ResourceId)))
            {
                throw new InvalidDataException($"Duplicate research cost: {row.ResearchId}, {row.ResourceId}");
            }
            node.Costs.Add(new ResearchCost(row.ResourceId, row.Amount));
        }

        HashSet<(string ResearchId, ResearchEffectType Type, string TargetId)> effectKeys = new();
        foreach (ResearchEffectCsvHeader row in effectRows)
        {
            ResearchNodeDefinition node = GetResearchNode(nodesById, row.ResearchId);
            ResearchEffectType type = ParseEnum<ResearchEffectType>(row.EffectType, row.ResearchId);
            if (type == ResearchEffectType.UnlockRuneEffectCategory)
            {
                ParseEnum<RuneEffectCategory>(row.TargetId, row.ResearchId);
            }
            else if (!row.Value.HasValue)
            {
                throw new InvalidDataException($"Research effect value is missing: {row.ResearchId}");
            }
            float value = row.Value ?? 0f;
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidDataException($"Invalid research effect value: {row.ResearchId}");
            }
            if (!effectKeys.Add((row.ResearchId, type, row.TargetId)))
            {
                throw new InvalidDataException($"Duplicate research effect: {row.ResearchId}, {type}, {row.TargetId}");
            }
            node.Effects.Add(new ResearchEffect(type, row.TargetId, value));
        }

        Debug.Log($"Loaded {nodes.Count} research node definitions.");
        return nodes;
    }

    private static ResearchNodeDefinition GetResearchNode(
        Dictionary<string, ResearchNodeDefinition> nodesById,
        string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !nodesById.TryGetValue(id, out ResearchNodeDefinition node))
        {
            throw new InvalidDataException($"Research node does not exist: {id}");
        }
        if (node.NodeType != ResearchNodeType.Research)
        {
            throw new InvalidDataException($"Research data cannot reference a root node: {id}");
        }
        return node;
    }

    private static T ParseEnum<T>(string value, string researchId) where T : struct, Enum
    {
        if (!Enum.TryParse(value, out T result) || !Enum.IsDefined(typeof(T), result))
        {
            throw new InvalidDataException($"Invalid {typeof(T).Name}: {value}, research: {researchId}");
        }
        return result;
    }
}
