using System;
using UnityEngine;

/// <summary>
/// 보상 선택지 1개의 데이터. 룬석이면 runeStoneCount만, 패시브면 passive만 쓴다.
/// </summary>
[Serializable]
public class RewardOption
{
    public RewardType type;
    public PassiveData passive;
    public int runeStoneCount;
    public Sprite icon;
    public string title;
    public string description;

    public static RewardOption ForRuneStone(int count, Sprite icon)
    {
        return new RewardOption
        {
            type = RewardType.RuneStone,
            runeStoneCount = count,
            icon = icon,
            title = $"룬석 {count}개",
            description = $"선택 즉시 플레이어 주변에 룬석 {count}개를 흩뿌립니다."
        };
    }

    public static RewardOption ForPassive(PassiveData data)
    {
        int percent = Mathf.RoundToInt(data.valuePerStack * 100f);
        return new RewardOption
        {
            type = RewardType.Passive,
            passive = data,
            icon = data.icon,
            title = $"{data.displayName} +{percent}%",
            description = data.description
        };
    }
}
