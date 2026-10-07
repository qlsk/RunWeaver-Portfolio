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
    public TraitData trait;

    public static RewardOption ForRuneStone(int count, Sprite icon)
    {
        return new RewardOption
        {
            Type = RewardType.RuneStone,
            RuneStoneCount = count,
            Icon = icon,
            Title = $"룬석 {count}개",
            Description = $"선택 즉시 플레이어 주변에 룬석 {count}개를 흩뿌립니다."
        };
    }

    public static RewardOption ForPassive(PassiveData data)
    {
        int percent = Mathf.RoundToInt(data.ValuePerStack * 100f);
        return new RewardOption
        {
            Type = RewardType.Passive,
            Passive = data,
            Icon = data.Icon,
            Title = $"{data.DisplayName} +{percent}%",
            Description = data.Description
        };
    }

    public static RewardOption ForTrait(TraitData data)
    {
        return new RewardOption
        {
            type = RewardType.Trait,
            trait = data,
            icon = data.icon,
            title = data.displayName,
            description = data.description
        };
    }
}
