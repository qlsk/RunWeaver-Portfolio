using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 스킬, 연구, 자원 등 게임 진행과 관련된 정의들의 집합
/// </summary>
public class GameDefinitionSet
{
    private IReadOnlyList<SkillNodeDefinition> _skillNodes;
    private IReadOnlyList<ResearchNodeDefinition> _researches;
    private GoldDefinitionSO _gold;
    private IReadOnlyList<ResourceDefinitionSO> _resources;

    public IReadOnlyList<SkillNodeDefinition> SkillNodes => _skillNodes;
    public IReadOnlyList<ResearchNodeDefinition> Researches => _researches;
    public GoldDefinitionSO Gold => _gold;
    public IReadOnlyList<ResourceDefinitionSO> Resources => _resources;

    public GameDefinitionSet(
        IReadOnlyList<SkillNodeDefinition> skillNodes,
        IReadOnlyList<ResearchNodeDefinition> researches,
        GoldDefinitionSO gold,
        IReadOnlyList<ResourceDefinitionSO> resources)
    {
        Check(skillNodes, nameof(skillNodes));
        Check(researches, nameof(researches));
        Check(gold, nameof(gold));
        Check(resources, nameof(resources));

        _skillNodes = skillNodes;
        _researches = researches;
        _gold = gold;
        _resources = resources;
    }

    private static void Check(Object target, string name)
    {
        if (target == null)
        {
            throw new MissingReferenceException(name);
        }
    }

    private static void Check<T>(IReadOnlyList<T> targets, string name) where T : class
    {
        if (targets == null)
        {
            throw new MissingReferenceException(name);
        }

        for (int i = 0; i < targets.Count; i++)
        {
            T target = targets[i];
            if (target == null || (target is Object unityTarget && unityTarget == null))
            {
                throw new MissingReferenceException($"{name}[{i}]");
            }
        }
    }
}
