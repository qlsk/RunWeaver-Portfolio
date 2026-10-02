using System;
using System.Collections.Generic;

/// <summary>
/// 연구 정의(ResearchNodeDefinition)와 진행 상태, 보유 재료로 화면 표시용 데이터를 만든다.
/// 가짜 데이터 소스와 실제 데이터 소스가 같은 규칙으로 화면 데이터를 만들도록 공용으로 사용한다.
/// - 분류용 루트(Root) 하나가 화면의 한 줄이 되고, 그 아래 연구들이 선행 연구 순서대로 단계가 된다.
/// - 상태: 연구 완료 > 선행 연구 미완료(잠김) > 재료 부족 > 연구 가능
/// </summary>
public static class ResearchViewDataBuilder
{
    public static void Build(
        IReadOnlyList<ResearchNodeDefinition> definitions,
        Func<string, bool> isResearched,
        Func<string, int> getResourceAmount,
        List<ResearchTrackData> tracks,
        Dictionary<string, ResearchNodeData> nodes)
    {
        tracks.Clear();
        nodes.Clear();
        if (definitions == null)
        {
            return;
        }

        Dictionary<string, ResearchNodeDefinition> byId = new();
        foreach (ResearchNodeDefinition definition in definitions)
        {
            if (definition != null && !string.IsNullOrWhiteSpace(definition.Id))
            {
                byId[definition.Id] = definition;
            }
        }

        foreach (ResearchNodeDefinition root in definitions)
        {
            if (root == null || root.NodeType != ResearchNodeType.Root)
            {
                continue;
            }

            List<ResearchNodeDefinition> steps = OrderSteps(root.Id, definitions);
            List<string> nodeIds = new(steps.Count);
            string trackName = ResearchText.GetTrackName(root.Id, root.DisplayName);
            string categoryName = ResearchText.GetCategoryName(root.Category);

            ResearchEffectType? trackEffectType = null;

            for (int i = 0; i < steps.Count; i++)
            {
                ResearchNodeDefinition step = steps[i];
                int stepNumber = i + 1;
                bool researched = isResearched(step.Id);

                ResearchEffect? effect = step.Effects != null && step.Effects.Count > 0
                    ? (ResearchEffect?)step.Effects[0]
                    : null;
                trackEffectType ??= effect?.Type;

                string prerequisiteName = string.Empty;
                bool prerequisiteMet = true;
                if (step.PrerequisiteIds != null)
                {
                    foreach (string prerequisiteId in step.PrerequisiteIds)
                    {
                        if (!isResearched(prerequisiteId))
                        {
                            prerequisiteMet = false;
                        }

                        if (byId.TryGetValue(prerequisiteId, out ResearchNodeDefinition prerequisite))
                        {
                            prerequisiteName = GetNodeName(prerequisite, steps, trackName);
                        }
                    }
                }

                List<ResearchCostData> costs = new();
                if (step.Costs != null)
                {
                    foreach (ResearchCost cost in step.Costs)
                    {
                        costs.Add(new ResearchCostData(cost.ResourceId, ResearchText.GetResourceName(cost.ResourceId),
                            cost.Amount, getResourceAmount(cost.ResourceId)));
                    }
                }

                ResearchNodeState state = GetState(researched, prerequisiteMet, costs);

                // 누적 효과: 연구 완료 노드는 현재 누적값, 아직이면 "현재  >  연구 후" 로 표시한다.
                string totalEffect = string.Empty;
                if (effect.HasValue)
                {
                    string before = ResearchText.GetTotalEffect(effect.Value.Type, CountTotal(steps, isResearched), steps.Count);
                    string after = ResearchText.GetTotalEffect(effect.Value.Type,
                        CountTotal(steps, id => isResearched(id) || id == step.Id), steps.Count);
                    totalEffect = researched ? before : $"{before}  >  {after}";
                }

                nodes[step.Id] = new ResearchNodeData
                {
                    Id = step.Id,
                    TrackId = root.Id,
                    Step = stepNumber,
                    StepCount = steps.Count,
                    DisplayName = $"{trackName} {ResearchText.GetRoman(stepNumber)}",
                    StepName = ResearchText.GetStepName(root.Id, stepNumber),
                    CategoryName = categoryName,
                    Description = ResearchText.GetTrackDescription(root.Id, step.Description),
                    StepEffect = effect.HasValue ? ResearchText.GetStepEffect(effect.Value) : string.Empty,
                    TotalEffect = totalEffect,
                    PrerequisiteName = prerequisiteName,
                    IsPrerequisiteMet = prerequisiteMet,
                    Costs = costs,
                    State = state
                };

                nodeIds.Add(step.Id);
            }

            tracks.Add(new ResearchTrackData
            {
                Id = root.Id,
                DisplayName = trackName,
                Subtitle = ResearchText.GetTrackSubtitle(root.Id),
                CategoryName = categoryName,
                TotalText = trackEffectType.HasValue
                    ? ResearchText.GetTotalEffect(trackEffectType.Value, CountTotal(steps, isResearched), steps.Count)
                    : string.Empty,
                NodeIds = nodeIds
            });
        }
    }

    private static ResearchNodeState GetState(bool researched, bool prerequisiteMet, List<ResearchCostData> costs)
    {
        if (researched)
        {
            return ResearchNodeState.Researched;
        }

        if (!prerequisiteMet)
        {
            return ResearchNodeState.Locked;
        }

        foreach (ResearchCostData cost in costs)
        {
            if (!cost.IsEnough)
            {
                return ResearchNodeState.NotEnough;
            }
        }

        return ResearchNodeState.Available;
    }

    /// <summary>
    /// 연구한 단계들의 효과 합. 카테고리 해금처럼 값이 없는 효과는 해금한 개수를 센다.
    /// </summary>
    private static float CountTotal(List<ResearchNodeDefinition> steps, Func<string, bool> isResearched)
    {
        float total = 0f;
        foreach (ResearchNodeDefinition step in steps)
        {
            if (!isResearched(step.Id) || step.Effects == null || step.Effects.Count == 0)
            {
                continue;
            }

            total += GetEffectAmount(step.Effects[0]);
        }

        return total;
    }

    private static float GetEffectAmount(ResearchEffect? effect)
    {
        if (!effect.HasValue)
        {
            return 0f;
        }

        return effect.Value.Type == ResearchEffectType.UnlockRuneEffectCategory ? 1f : effect.Value.Value;
    }

    private static string GetNodeName(ResearchNodeDefinition node, List<ResearchNodeDefinition> steps, string trackName)
    {
        int index = steps.IndexOf(node);
        return index >= 0 ? $"{trackName} {ResearchText.GetRoman(index + 1)}" : node.DisplayName;
    }

    /// <summary>
    /// 같은 루트의 연구들을 선행 연구 순서대로 정렬한다. (선행 연구가 같은 줄 안에 먼저 오도록)
    /// </summary>
    private static List<ResearchNodeDefinition> OrderSteps(string rootId, IReadOnlyList<ResearchNodeDefinition> definitions)
    {
        List<ResearchNodeDefinition> remaining = new();
        foreach (ResearchNodeDefinition definition in definitions)
        {
            if (definition != null && definition.NodeType == ResearchNodeType.Research && definition.RootId == rootId)
            {
                remaining.Add(definition);
            }
        }

        List<ResearchNodeDefinition> ordered = new(remaining.Count);
        HashSet<string> placed = new();
        while (remaining.Count > 0)
        {
            int index = remaining.FindIndex(node => node.PrerequisiteIds == null ||
                node.PrerequisiteIds.TrueForAll(id => placed.Contains(id) || !remaining.Exists(other => other.Id == id)));
            if (index < 0)
            {
                index = 0;
            }

            ResearchNodeDefinition next = remaining[index];
            remaining.RemoveAt(index);
            ordered.Add(next);
            placed.Add(next.Id);
        }

        return ordered;
    }
}
