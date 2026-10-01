using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 업그레이드 UI에 표시할 비용 한 줄 (재료 한 종류)
/// </summary>
public sealed class SkillCostData
{
    public string ResourceId { get; }
    public string DisplayName { get; }
    public Sprite Icon { get; }
    public int Required { get; }
    public int Owned { get; }

    public bool IsEnough => Owned >= Required;

    public SkillCostData(string resourceId, string displayName, Sprite icon, int required, int owned)
    {
        ResourceId = resourceId;
        DisplayName = displayName;
        Icon = icon;
        Required = required;
        Owned = owned;
    }
}

/// <summary>
/// 스킬 업그레이드 UI에 표시할 성장 노드 한 개의 정보
/// </summary>
public sealed class SkillNodeData
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public int Level { get; }
    public int MaxLevel { get; }
    public bool IsPrerequisiteMet { get; }

    /// <summary>선행 노드 이름. 선행 노드가 없으면 빈 문자열</summary>
    public string PrerequisiteName { get; }

    /// <summary>아직 데이터가 정해지지 않은 노드 (예: 정화)</summary>
    public bool IsComingSoon { get; }

    /// <summary>다음 레벨 비용. 최대 레벨이거나 준비 중이면 비어 있다.</summary>
    public IReadOnlyList<SkillCostData> Costs { get; }

    public bool IsMaxLevel => !IsComingSoon && Level >= MaxLevel;
    public float LevelRatio => MaxLevel > 0 ? Mathf.Clamp01((float)Level / MaxLevel) : 0f;

    public SkillNodeData(string id, string displayName, string description, int level, int maxLevel,
        bool isPrerequisiteMet, string prerequisiteName, IReadOnlyList<SkillCostData> costs)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Level = level;
        MaxLevel = maxLevel;
        IsPrerequisiteMet = isPrerequisiteMet;
        PrerequisiteName = prerequisiteName ?? string.Empty;
        Costs = costs ?? Array.Empty<SkillCostData>();
    }

    private SkillNodeData(string id, string displayName, string description)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        IsPrerequisiteMet = true;
        PrerequisiteName = string.Empty;
        IsComingSoon = true;
        Costs = Array.Empty<SkillCostData>();
    }

    public static SkillNodeData CreateComingSoon(string id, string displayName, string description)
    {
        return new SkillNodeData(id, displayName, description);
    }
}

/// <summary>
/// 가운데 큰 노드(스킬 본체)를 눌렀을 때 보여줄 스킬 소개
/// </summary>
public sealed class SkillIntroData
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public SkillIntroData(string id, string displayName, string description)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
    }
}
