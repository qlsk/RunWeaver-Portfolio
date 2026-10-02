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
            ReactionElement.Fire, new ElementReactionData("elemental_fire", "Fire", ReactionElement.Fire, null,
                "Sets enemies on fire, dealing damage over time.", "Burn: damage over time")
        },
        {
            ReactionElement.Water, new ElementReactionData("elemental_water", "Water", ReactionElement.Water, null,
                "Reduces the movement speed of enemies.", "Slow: reduces move speed")
        },
        {
            ReactionElement.Earth, new ElementReactionData("elemental_earth", "Earth", ReactionElement.Earth, null,
                "Staggers enemies, interrupting their current action and stopping them briefly. Can cancel attack wind-ups.",
                "Stagger: interrupts actions")
        },
        {
            ReactionElement.Wind, new ElementReactionData("elemental_wind", "Wind", ReactionElement.Wind, null,
                "Pushes enemies hit in the direction the bullet is travelling.", "Knockback: pushes enemies away")
        }
    };

    private static readonly List<ElementReactionData> s_reactions = new()
    {
        new("reaction_fire_fire", "Overheat", ReactionElement.Fire, ReactionElement.Fire,
            "Explodes at the point of impact and leaves embers that keep damaging enemies over time.", string.Empty),
        new("reaction_water_water", "Deluge", ReactionElement.Water, ReactionElement.Water,
            "Floods a wide area that slows enemies and pushes them back. The same debuff does not stack.", string.Empty),
        new("reaction_earth_earth", "Petrify", ReactionElement.Earth, ReactionElement.Earth,
            "Heavily slows or binds enemies and weakens their defense.", string.Empty),
        new("reaction_wind_wind", "Cyclone", ReactionElement.Wind, ReactionElement.Wind,
            "Creates a whirlwind that pulls nearby enemies in and damages them over time. Knocks them back when it ends.",
            string.Empty),
        new("reaction_fire_water", "Steam", ReactionElement.Fire, ReactionElement.Water,
            "Creates a cloud of steam that damages enemies over time and makes them update your position less often.",
            string.Empty),
        new("reaction_fire_earth", "Molten", ReactionElement.Fire, ReactionElement.Earth,
            "Creates a field of lava that damages enemies over time and blocks movement through the area.", string.Empty),
        new("reaction_fire_wind", "Firestorm", ReactionElement.Fire, ReactionElement.Wind,
            "Widens the area of the flames and spreads burning to nearby enemies.", string.Empty),
        new("reaction_water_earth", "Mire", ReactionElement.Water, ReactionElement.Earth,
            "Creates a mud zone that greatly slows enemies. Enemies that stay in it too long become bound.", string.Empty),
        new("reaction_water_wind", "Tempest", ReactionElement.Water, ReactionElement.Wind,
            "Creates a whirlpool that repeatedly damages enemies and controls where they move.", string.Empty),
        new("reaction_earth_wind", "Sandstorm", ReactionElement.Earth, ReactionElement.Wind,
            "Creates a sandstorm that damages enemies over time and lowers their defense.", string.Empty)
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
