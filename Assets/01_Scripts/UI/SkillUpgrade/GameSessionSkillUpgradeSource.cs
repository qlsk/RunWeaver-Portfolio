using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실제 게임 데이터(GameSession의 SkillTree, 거점 보관 재료, SkillDatabase)를 스킬 업그레이드 UI에 연결한다.
/// BootStrap을 거쳐 GameSession이 준비된 상태에서 사용한다.
/// </summary>
public class GameSessionSkillUpgradeSource : SkillUpgradeDataSource
{
    private PlayerProgression _progression;
    private GameSession _session;

    private SkillTree SkillTree => _progression?.SkillTree;
    private ResourceInventory Inventory => _progression?.Holdings?.StoredResources;

    private void OnEnable()
    {
        _session = GameSession.Instance;
        if (_session == null || !_session.IsReady)
        {
            Debug.LogWarning("[GameSessionSkillUpgradeSource] GameSession이 준비되지 않았습니다. BootStrap을 거쳐 실행하세요.", this);
            return;
        }

        _session.ProgressReplaced += HandleProgressReplaced;
        Bind(_session.Current);
    }

    private void OnDisable()
    {
        if (_session != null)
        {
            _session.ProgressReplaced -= HandleProgressReplaced;
        }

        Bind(null);
        _session = null;
    }

    public override SkillNodeData GetNode(string nodeId)
    {
        SkillTree tree = SkillTree;
        if (tree == null || !tree.ContainsNode(nodeId))
        {
            return CreateComingSoon(nodeId);
        }

        GetNodeText(nodeId, out string name, out string description);

        string prerequisiteId = tree.GetPrerequisiteNodeId(nodeId);
        string prerequisiteName = string.Empty;
        if (!string.IsNullOrWhiteSpace(prerequisiteId))
        {
            GetNodeText(prerequisiteId, out prerequisiteName, out _);
        }

        List<SkillCostData> costs = new();
        if (!tree.IsMaxLevel(nodeId) && Inventory != null)
        {
            costs.Add(CreateCost(tree.GetUpgradeCost(nodeId), Inventory.GetQuantity(UpgradeResourceId)));
        }

        return new SkillNodeData(nodeId, name, description, tree.GetNodeLevel(nodeId), tree.GetMaxLevel(nodeId),
            tree.IsPrerequisiteMet(nodeId), prerequisiteName, costs);
    }

    public override SkillIntroData GetSkillIntro(string skillId)
    {
        SkillDatabase database = SkillDatabase.Instance;
        if (database != null && database.TryGetSkillById(skillId, out SkillDefinition skill))
        {
            return CreateIntro(skillId, skill.DisplayName, skill.Description);
        }

        return CreateIntro(skillId, null, null);
    }

    public override bool CanUpgrade(string nodeId)
    {
        SkillTree tree = SkillTree;
        return tree != null && Inventory != null && tree.ContainsNode(nodeId)
               && tree.CanUpgrade(nodeId, Inventory, UpgradeResourceId);
    }

    public override bool TryUpgrade(string nodeId)
    {
        if (!CanUpgrade(nodeId))
        {
            return false;
        }

        // 레벨 변경과 재료 변경 이벤트로 Changed가 발생한다.
        return SkillTree.TryUpgrade(nodeId, Inventory, UpgradeResourceId);
    }

    private void HandleProgressReplaced(PlayerProgression progression)
    {
        Bind(progression);
    }

    private void Bind(PlayerProgression progression)
    {
        if (_progression != null)
        {
            _progression.SkillTree.NodeLevelChanged -= HandleNodeLevelChanged;
            _progression.Holdings.StoredResources.QuantitiesChanged -= NotifyChanged;
        }

        _progression = progression;

        if (_progression != null)
        {
            _progression.SkillTree.NodeLevelChanged += HandleNodeLevelChanged;
            _progression.Holdings.StoredResources.QuantitiesChanged += NotifyChanged;
        }

        NotifyChanged();
    }

    private void HandleNodeLevelChanged(string nodeId, int level)
    {
        NotifyChanged();
    }

    private static void GetNodeText(string nodeId, out string name, out string description)
    {
        if (SkillUpgradeText.TryGetNode(nodeId, out name, out description))
        {
            return;
        }

        SkillDatabase database = SkillDatabase.Instance;
        if (database != null && database.TryGetSkillNodeById(nodeId, out SkillNodeDefinition definition))
        {
            name = definition.DisplayName;
            description = definition.Description;
            return;
        }

        name = nodeId;
        description = string.Empty;
    }
}
