using System;
using UnityEngine;

/// <summary>
/// 보상 선택지 1개의 데이터. 룬석이면 runeStoneCount만, 패시브면 passive만 쓴다.
/// </summary>
[Serializable]
public class RewardOption
{
    public RewardType Type;
    public PassiveData Passive;
    public int RuneStoneCount;
    public Sprite Icon;
    public string Title;
    public string Description;
    public TraitData Trait;

    /// <summary>카드에 표시할 카테고리 텍스트 (룬석/패시브/특성/슬롯 확장).</summary>
    public string CategoryLabel;

    public static RewardOption ForRuneStone(int count, Sprite icon)
    {
        return ForRuneStone(count, icon, "룬석 {0}개", "선택 즉시 플레이어 주변에 룬석 {0}개를 흩뿌립니다.");
    }

    public static RewardOption ForRuneStone(int count, Sprite icon, string titleFormat, string descriptionFormat)
    {
        if (string.IsNullOrWhiteSpace(titleFormat))
        {
            titleFormat = "룬석 {0}개";
        }

        if (string.IsNullOrWhiteSpace(descriptionFormat))
        {
            descriptionFormat = "선택 즉시 플레이어 주변에 룬석 {0}개를 흩뿌립니다.";
        }

        return new RewardOption
        {
            Type = RewardType.RuneStone,
            RuneStoneCount = count,
            Icon = icon,
            Title = string.Format(titleFormat, count),
            Description = string.Format(descriptionFormat, count),
            CategoryLabel = "룬석"
        };
    }

    /// <summary>
    /// currentStacks = 현재 보유 레벨. 선택 시 도달할 레벨과 그때 총합을 미리 보여준다.
    /// 예: 1레벨 보유 중이면 제목 "데미지 2레벨", 설명 "데미지 +5% (10%)".
    /// </summary>
    public static RewardOption ForPassive(PassiveData data, int currentStacks)
    {
        int nextLevel = Mathf.Max(0, currentStacks) + 1;
        int percent = Mathf.RoundToInt(data.ValuePerStack * 100f);
        int totalPercent = Mathf.RoundToInt(data.ValuePerStack * nextLevel * 100f);
        string name = string.IsNullOrWhiteSpace(data.DisplayName)
            ? GetDefaultPassiveName(data.StatType)
            : data.DisplayName;
        string description = string.IsNullOrWhiteSpace(data.Description)
            ? $"{name} +{percent}% ({totalPercent}%)"
            : $"{data.Description} ({totalPercent}%)";
        return new RewardOption
        {
            Type = RewardType.Passive,
            Passive = data,
            Icon = data.Icon,
            Title = $"{name} {nextLevel}레벨",
            Description = description,
            CategoryLabel = "패시브"
        };
    }

    private static string GetDefaultPassiveName(PassiveStatType statType)
    {
        return statType switch
        {
            PassiveStatType.Damage => "데미지",
            PassiveStatType.AttackSpeed => "공격속도",
            PassiveStatType.MoveSpeed => "이동속도",
            PassiveStatType.MagazineSize => "탄창 크기",
            _ => statType.ToString()
        };
    }

    public static RewardOption ForTrait(TraitData data)
    {
        return new RewardOption
        {
            Type = RewardType.Trait,
            Trait = data,
            Icon = data.Icon,
            Title = data.DisplayName,
            Description = data.Description,
            CategoryLabel = "특성"
        };
    }

    public static RewardOption ForSlotExpansion(int currentSlots, int maxSlots, Sprite icon)
    {
        return new RewardOption
        {
            Type = RewardType.SlotExpansion,
            Icon = icon,
            Title = $"룬 슬롯 확장 ({currentSlots}→{Mathf.Min(maxSlots, currentSlots + 1)}칸)",
            Description = "룬 장착 슬롯이 1칸 열린다.",
            CategoryLabel = "슬롯 확장"
        };
    }
}
