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
            Description = string.Format(descriptionFormat, count)
        };
    }

    public static RewardOption ForPassive(PassiveData data)
    {
        int percent = Mathf.RoundToInt(data.ValuePerStack * 100f);
        string name = string.IsNullOrWhiteSpace(data.DisplayName)
            ? GetDefaultPassiveName(data.StatType)
            : data.DisplayName;
        string description = string.IsNullOrWhiteSpace(data.Description)
            ? $"{name} +{percent}%"
            : data.Description;
        return new RewardOption
        {
            Type = RewardType.Passive,
            Passive = data,
            Icon = data.Icon,
            Title = $"{name} +{percent}%",
            Description = description
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
            Description = data.Description
        };
    }
}
