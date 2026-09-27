using System;
using System.Collections.Generic;

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
        _skillNodes = skillNodes ?? throw new ArgumentNullException(nameof(skillNodes));
        _researches = researches ?? throw new ArgumentNullException(nameof(researches));
        _gold = gold;
        _resources = resources;
    }
}
