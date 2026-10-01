using System;
using UnityEngine;

/// <summary>
/// 스킬 업그레이드 UI가 읽는 데이터 창구.
/// 실제 게임에서는 GameSessionSkillUpgradeSource, 단독 테스트에서는 FakeSkillUpgradeDataSource를 사용한다.
/// </summary>
public abstract class SkillUpgradeDataSource : MonoBehaviour
{
    [Serializable]
    private struct ResourceIconEntry
    {
        public string ResourceId;
        public Sprite Icon;
    }

    [Header("업그레이드에 사용하는 재료 (DB에 재료 종류가 정해지기 전까지 임시)")]
    [SerializeField] private string _upgradeResourceId = "mercury";

    [Header("재료 아이콘 (비워두면 아이콘 칸의 기본 이미지를 사용)")]
    [SerializeField] private ResourceIconEntry[] _resourceIcons = Array.Empty<ResourceIconEntry>();

    /// <summary>
    /// 레벨, 재료 수량 등 표시할 값이 바뀌었을 때 발생한다.
    /// </summary>
    public event Action Changed;

    protected string UpgradeResourceId => _upgradeResourceId;

    /// <summary>
    /// 성장 노드 정보. 데이터에 없는 노드는 "준비 중" 노드로 돌려준다.
    /// </summary>
    public abstract SkillNodeData GetNode(string nodeId);

    /// <summary>
    /// 가운데 큰 노드(스킬 본체)의 소개
    /// </summary>
    public abstract SkillIntroData GetSkillIntro(string skillId);

    public abstract bool CanUpgrade(string nodeId);

    public abstract bool TryUpgrade(string nodeId);

    protected void NotifyChanged()
    {
        SafeEventNotifier.Notify(Changed);
    }

    protected SkillCostData CreateCost(int required, int owned)
    {
        return new SkillCostData(_upgradeResourceId, SkillUpgradeText.GetResourceName(_upgradeResourceId),
            FindResourceIcon(_upgradeResourceId), required, owned);
    }

    protected static SkillIntroData CreateIntro(string skillId, string fallbackName, string fallbackDescription)
    {
        if (SkillUpgradeText.TryGetSkill(skillId, out string name, out string description))
        {
            return new SkillIntroData(skillId, name, description);
        }

        return new SkillIntroData(skillId, fallbackName ?? skillId, fallbackDescription ?? string.Empty);
    }

    protected static SkillNodeData CreateComingSoon(string nodeId)
    {
        return SkillNodeData.CreateComingSoon(nodeId, SkillUpgradeText.ComingSoonName,
            SkillUpgradeText.ComingSoonDescription);
    }

    private Sprite FindResourceIcon(string resourceId)
    {
        foreach (ResourceIconEntry entry in _resourceIcons)
        {
            if (entry.ResourceId == resourceId)
            {
                return entry.Icon;
            }
        }

        return null;
    }
}
