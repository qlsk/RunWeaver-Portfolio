using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 연구 화면 단독 테스트용 데이터 소스. (인터넷, BootStrap 없이 실행)
/// 연구 DB(Rune Weaver Research Database) 내용을 그대로 옮겨 두었고,
/// 시작 재료와 이미 연구한 항목은 인스펙터에서 정한다.
/// 실제 게임에서는 GameSession의 연구 트리와 거점 보관 재료를 쓰는 소스로 교체한다.
/// </summary>
public class FakeResearchDataSource : ResearchDataSource
{
    [System.Serializable]
    private class ResourceAmount
    {
        public string resourceId;
        [Min(0)] public int amount;
    }

    [SerializeField] private List<ResourceAmount> _startingResources = new()
    {
        new ResourceAmount { resourceId = "prima_materia", amount = 350 },
        new ResourceAmount { resourceId = "mercury", amount = 80 },
        new ResourceAmount { resourceId = "aether_crystal", amount = 40 },
        new ResourceAmount { resourceId = "azoth", amount = 1 }
    };

    [SerializeField] private List<string> _researchedAtStart = new()
    {
        "research_rune_01_01",
        "research_rune_01_02",
        "research_rune_02_01",
        "research_spirit_01_01"
    };

    private readonly Dictionary<string, int> _resources = new();
    private readonly HashSet<string> _researched = new();
    private readonly List<ResearchTrackData> _tracks = new();
    private readonly Dictionary<string, ResearchNodeData> _nodes = new();
    private List<ResearchNodeDefinition> _definitions;

    private void Awake()
    {
        _definitions = CreateDefinitions();

        foreach (ResourceAmount resource in _startingResources)
        {
            if (resource != null && !string.IsNullOrWhiteSpace(resource.resourceId))
            {
                _resources[resource.resourceId] = resource.amount;
            }
        }

        foreach (string researchId in _researchedAtStart)
        {
            if (!string.IsNullOrWhiteSpace(researchId))
            {
                _researched.Add(researchId);
            }
        }

        Rebuild();
    }

    public override IReadOnlyList<ResearchTrackData> GetTracks()
    {
        return _tracks;
    }

    public override ResearchNodeData GetNode(string researchId)
    {
        return researchId != null && _nodes.TryGetValue(researchId, out ResearchNodeData node) ? node : null;
    }

    public override int GetResourceAmount(string resourceId)
    {
        return resourceId != null && _resources.TryGetValue(resourceId, out int amount) ? amount : 0;
    }

    public override bool TryResearch(string researchId)
    {
        ResearchNodeData node = GetNode(researchId);
        if (node == null || node.State != ResearchNodeState.Available)
        {
            return false;
        }

        foreach (ResearchCostData cost in node.Costs)
        {
            _resources[cost.ResourceId] = GetResourceAmount(cost.ResourceId) - cost.Required;
        }

        _researched.Add(researchId);
        Debug.Log($"[FakeResearchDataSource] Researched {node.DisplayName}", this);

        Rebuild();
        RaiseChanged();
        return true;
    }

    private void Rebuild()
    {
        ResearchViewDataBuilder.Build(_definitions, id => _researched.Contains(id), GetResourceAmount, _tracks, _nodes);
    }

    // ---- 연구 DB 내용 (ResearchNode / ResearchPrerequisite / ResearchCost / ResearchEffect 시트) ----

    private static List<ResearchNodeDefinition> CreateDefinitions()
    {
        List<ResearchNodeDefinition> list = new();

        AddTrack(list, "research_rune_01", ResearchCategory.RuneUtilization, "mercury",
            ResearchEffectType.IncreaseRuneActivationLimit, new[] { "", "", "" }, new[] { 1f, 1f, 1f });
        AddTrack(list, "research_rune_02", ResearchCategory.RuneUtilization, "mercury",
            ResearchEffectType.UnlockRuneEffectCategory, new[] { "Behaviour", "Control", "Special" }, new[] { 0f, 0f, 0f });
        AddTrack(list, "research_spirit_01", ResearchCategory.SpiritRealm, "aether_crystal",
            ResearchEffectType.ReduceDepthPenalty, new[] { "", "", "" }, new[] { 0.05f, 0.05f, 0.10f });
        AddTrack(list, "research_spirit_02", ResearchCategory.SpiritRealm, "aether_crystal",
            ResearchEffectType.IncreaseItemDropRate, new[] { "", "", "" }, new[] { 0.05f, 0.05f, 0.10f });
        AddTrack(list, "research_conversion_01", ResearchCategory.Conversion, "mercury",
            ResearchEffectType.IncreaseConversionEfficiency, new[] { "", "", "" }, new[] { 0.10f, 0.10f, 0.15f });

        return list;
    }

    /// <summary>
    /// 루트 하나와 3단계 연구를 추가한다. 비용 규칙(DB 기준):
    /// 1단계 prima_materia 100 + 줄 재료 20, 2단계 prima_materia 250 + 줄 재료 50, 3단계 azoth 2 + 줄 재료 100
    /// </summary>
    private static void AddTrack(List<ResearchNodeDefinition> list, string rootId, ResearchCategory category,
        string trackResourceId, ResearchEffectType effectType, string[] targets, float[] values)
    {
        list.Add(new ResearchNodeDefinition(rootId, rootId, category, string.Empty, ResearchNodeType.Root, string.Empty,
            new List<string>(), new List<ResearchCost>(), new List<ResearchEffect>()));

        int[] primaryAmounts = { 100, 250, 2 };
        string[] primaryIds = { "prima_materia", "prima_materia", "azoth" };
        int[] trackAmounts = { 20, 50, 100 };

        for (int i = 0; i < 3; i++)
        {
            string id = $"{rootId}_{i + 1:00}";
            List<string> prerequisites = i == 0 ? new List<string>() : new List<string> { $"{rootId}_{i:00}" };
            List<ResearchCost> costs = new()
            {
                new ResearchCost(primaryIds[i], primaryAmounts[i]),
                new ResearchCost(trackResourceId, trackAmounts[i])
            };
            List<ResearchEffect> effects = new() { new ResearchEffect(effectType, targets[i], values[i]) };

            list.Add(new ResearchNodeDefinition(id, id, category, string.Empty, ResearchNodeType.Research, rootId,
                prerequisites, costs, effects));
        }
    }
}
