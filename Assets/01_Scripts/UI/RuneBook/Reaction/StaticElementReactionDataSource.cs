using System.Collections.Generic;

/// <summary>
/// 원소 반응 도감 데이터를 코드에 담아 둔 소스. (DB의 ElementalEffect 시트 내용을 영문으로 옮김)
/// 피해량, 지속시간은 아직 미정이라 비워 둔다. (비어 있으면 화면에서 숨김)
/// </summary>
public class StaticElementReactionDataSource : ElementReactionDataSource
{
    private static readonly Dictionary<ReactionElement, ElementReactionData> s_baseEffects = new()
    {
        {
            ReactionElement.Fire, new ElementReactionData("elemental_fire", "불", ReactionElement.Fire, null,
                "적에게 화상을 입혀 지속 피해를 줍니다.", "화상: 지속 피해")
        },
        {
            ReactionElement.Water, new ElementReactionData("elemental_water", "물", ReactionElement.Water, null,
                "적의 이동 속도를 감소시킵니다.", "감속: 이동 속도 감소")
        },
        {
            ReactionElement.Earth, new ElementReactionData("elemental_earth", "흙", ReactionElement.Earth, null,
                "적의 현재 행동을 끊고 잠시 멈추게 합니다. 공격 준비 동작도 끊을 수 있습니다.",
                "경직: 행동 중단")
        },
        {
            ReactionElement.Wind, new ElementReactionData("elemental_wind", "바람", ReactionElement.Wind, null,
                "맞은 적을 탄환이 날아가는 방향으로 밀쳐냅니다.", "넉백: 적을 밀쳐냄")
        }
    };

    private static readonly List<ElementReactionData> s_reactions = new()
    {
        new("reaction_fire_fire", "과열", ReactionElement.Fire, ReactionElement.Fire,
            "적중 지점에서 폭발하고 잔불을 남겨 지속 피해를 줍니다.", string.Empty),
        new("reaction_water_water", "범람", ReactionElement.Water, ReactionElement.Water,
            "넓은 범위를 물로 덮어 적을 둔화시키고 밀어냅니다. 같은 디버프는 중첩되지 않습니다.", string.Empty),
        new("reaction_earth_earth", "석화", ReactionElement.Earth, ReactionElement.Earth,
            "적을 강하게 둔화시키거나 속박하고 방어를 약화시킵니다.", string.Empty),
        new("reaction_wind_wind", "회오리", ReactionElement.Wind, ReactionElement.Wind,
            "회오리를 만들어 주변 적을 끌어당기고 지속 피해를 줍니다. 끝날 때 적을 밀쳐냅니다.",
            string.Empty),
        new("reaction_fire_water", "증기", ReactionElement.Fire, ReactionElement.Water,
            "증기 구름을 만들어 지속 피해를 주고, 적이 플레이어 위치를 덜 자주 추적하게 합니다.",
            string.Empty),
        new("reaction_fire_earth", "용융", ReactionElement.Fire, ReactionElement.Earth,
            "용암 지대를 만들어 지속 피해를 주고 그 지역의 이동을 막습니다.", string.Empty),
        new("reaction_fire_wind", "화염폭풍", ReactionElement.Fire, ReactionElement.Wind,
            "화염의 범위를 넓히고 주변 적에게 화상을 퍼뜨립니다.", string.Empty),
        new("reaction_water_earth", "진흙", ReactionElement.Water, ReactionElement.Earth,
            "진흙 지대를 만들어 적을 크게 둔화시킵니다. 오래 머문 적은 속박됩니다.", string.Empty),
        new("reaction_water_wind", "폭풍우", ReactionElement.Water, ReactionElement.Wind,
            "소용돌이를 만들어 반복해서 피해를 주고 적의 움직임을 제어합니다.", string.Empty),
        new("reaction_earth_wind", "모래폭풍", ReactionElement.Earth, ReactionElement.Wind,
            "모래폭풍을 일으켜 지속 피해를 주고 적의 방어력을 낮춥니다.", string.Empty)
    };

    public override ElementReactionData GetBaseEffect(ReactionElement element)
    {
        return s_baseEffects.TryGetValue(element, out ElementReactionData data) ? data : null;
    }

    public override ElementReactionData GetReaction(ReactionElement a, ReactionElement b)
    {
        foreach (ElementReactionData reaction in s_reactions)
        {
            if ((reaction.ElementA == a && reaction.ElementB == b) || (reaction.ElementA == b && reaction.ElementB == a))
            {
                return reaction;
            }
        }

        return null;
    }
}
