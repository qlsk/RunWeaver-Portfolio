using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [테스트 전용] SkillUpgradeScene을 단독으로 실행할 때 사용하는 가짜 데이터.
/// 기획서 5-6의 성장 노드(최대 레벨, 비용, 선행 노드)로 실제 SkillTree를 만들어 레벨 규칙을 그대로 검증한다.
/// 재료는 ResourceInventory 대신 이 컴포넌트가 직접 보관한다. (ResourceDefinitionSO 없이 실행하기 위함)
///
/// 테스트 키
/// 1: 재료 추가 / 2: 레벨과 재료 초기화
/// </summary>
public class FakeSkillUpgradeDataSource : SkillUpgradeDataSource
{
    private static readonly (string Id, string SkillId, string PrerequisiteId, int MaxLevel, int Cost)[] s_nodeTable =
    {
        ("attack_01", "attack", "", 5, 3),
        ("attack_02", "attack", "", 5, 3),
        ("attack_03", "attack", "", 5, 3),
        ("attack_04", "attack", "attack_01", 3, 10),
        ("attack_05", "attack", "attack_01", 3, 10),
        ("dash_01", "dash", "", 5, 3),
        ("dash_02", "dash", "", 5, 3),
        ("dash_03", "dash", "", 5, 3),
        ("dash_04", "dash", "dash_01", 3, 10),
        ("dash_05", "dash", "dash_03", 3, 10),
        ("shield_01", "shield", "", 3, 3),
        ("shield_02", "shield", "", 5, 3),
        ("shield_03", "shield", "shield_01", 3, 10),
        ("shield_04", "shield", "shield_03", 3, 10),
        ("shield_05", "shield", "shield_02", 3, 10)
    };

    [Header("테스트 재료")]
    [SerializeField, Min(0)] private int _startQuantity = 20;
    [SerializeField, Min(1)] private int _addQuantity = 20;

    [Header("테스트 키")]
    [SerializeField] private KeyCode _addResourceKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _resetKey = KeyCode.Alpha2;

    private SkillTree _skillTree;
    private int _quantity;

    private SkillTree Tree
    {
        get
        {
            EnsureInitialized();
            return _skillTree;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_addResourceKey))
        {
            AddResource();
        }

        if (Input.GetKeyDown(_resetKey))
        {
            ResetProgress();
        }
    }

    public override SkillNodeData GetNode(string nodeId)
    {
        SkillTree tree = Tree;
        if (!tree.ContainsNode(nodeId))
        {
            return CreateComingSoon(nodeId);
        }

        SkillUpgradeText.TryGetNode(nodeId, out string name, out string description);

        string prerequisiteId = tree.GetPrerequisiteNodeId(nodeId);
        string prerequisiteName = string.Empty;
        if (!string.IsNullOrWhiteSpace(prerequisiteId))
        {
            SkillUpgradeText.TryGetNode(prerequisiteId, out prerequisiteName, out _);
        }

        List<SkillCostData> costs = new();
        if (!tree.IsMaxLevel(nodeId))
        {
            costs.Add(CreateCost(tree.GetUpgradeCost(nodeId), _quantity));
        }

        return new SkillNodeData(nodeId, name ?? nodeId, description ?? string.Empty, tree.GetNodeLevel(nodeId),
            tree.GetMaxLevel(nodeId), tree.IsPrerequisiteMet(nodeId), prerequisiteName, costs);
    }

    public override SkillIntroData GetSkillIntro(string skillId)
    {
        return CreateIntro(skillId, null, null);
    }

    public override bool CanUpgrade(string nodeId)
    {
        SkillTree tree = Tree;
        return tree.ContainsNode(nodeId) && tree.CanIncreaseLevel(nodeId) && _quantity >= tree.GetUpgradeCost(nodeId);
    }

    public override bool TryUpgrade(string nodeId)
    {
        if (!CanUpgrade(nodeId))
        {
            return false;
        }

        _quantity -= Tree.GetUpgradeCost(nodeId);
        Tree.IncreaseLevel(nodeId);
        Debug.Log($"[FakeSkillUpgradeDataSource] {nodeId} Lv.{Tree.GetNodeLevel(nodeId)} (남은 재료 {_quantity})");
        NotifyChanged();
        return true;
    }

    [ContextMenu("Test/Add Resource")]
    private void AddResource()
    {
        EnsureInitialized();
        _quantity += _addQuantity;
        Debug.Log($"[FakeSkillUpgradeDataSource] 재료 +{_addQuantity} (보유 {_quantity})");
        NotifyChanged();
    }

    [ContextMenu("Test/Reset Progress")]
    private void ResetProgress()
    {
        _skillTree = CreateTree();
        _quantity = _startQuantity;
        Debug.Log("[FakeSkillUpgradeDataSource] 레벨과 재료 초기화");
        NotifyChanged();
    }

    /// <summary>
    /// UI가 이 컴포넌트의 Awake보다 먼저 데이터를 요청할 수 있으므로, 처음 요청될 때 준비한다.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_skillTree != null)
        {
            return;
        }

        _skillTree = CreateTree();
        _quantity = _startQuantity;
    }

    private static SkillTree CreateTree()
    {
        List<SkillNodeDefinition> definitions = new(s_nodeTable.Length);
        foreach ((string id, string skillId, string prerequisiteId, int maxLevel, int cost) in s_nodeTable)
        {
            SkillUpgradeText.TryGetNode(id, out string name, out string description);
            definitions.Add(new SkillNodeDefinition(id, name, description, skillId, prerequisiteId, maxLevel, cost));
        }

        return new SkillTree(definitions);
    }
}
