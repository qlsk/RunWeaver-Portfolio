using System.Collections.Generic;

/// <summary>
/// 스킬 업그레이드 UI에 표시할 영문 문구 (TMP 폰트에 한글이 없어 영문으로 표시한다).
/// 기획서 5-5(스킬), 5-6(스킬 성장 노드), 13-2(연구 재료)를 기준으로 옮겼다.
/// DB의 표시명이 한글이므로, 여기에 없는 id만 DB 값을 그대로 사용한다.
/// </summary>
public static class SkillUpgradeText
{
    private static readonly Dictionary<string, (string Name, string Description)> s_skills = new()
    {
        { "attack", ("Basic Attack", "Fires a bullet toward the mouse cursor.") },
        { "dash", ("Dash", "Quickly moves in the movement direction. You are invincible while dashing.") },
        { "shield", ("Barrier", "Holds a shield charge that automatically blocks one hit, then recharges over time.") },
        { "cleanse", ("Cleanse", "Removes harmful effects. Details will be decided with the status effect system.") }
    };

    private static readonly Dictionary<string, (string Name, string Description)> s_nodes = new()
    {
        { "attack_01", ("Condensed Mana", "Attack damage +4 / 8 / 12 / 16 / 20%") },
        { "attack_02", ("Sigil of Haste", "Attack speed +3 / 6 / 9 / 12 / 15%") },
        { "attack_03", ("Elemental Surge", "Elemental reaction damage +4 / 8 / 12 / 16 / 20%") },
        { "attack_04", ("Echo Bullet", "Every 5 attacks, fires an extra bullet dealing 30 / 40 / 50% of base damage.") },
        { "attack_05", ("Hunter's Mark", "Hits mark the target for 3 seconds. Damage to marked targets +5 / 10 / 15%") },
        { "dash_01", ("Wind Step", "Dash distance +4 / 8 / 12 / 16 / 20%") },
        { "dash_02", ("Recovered Breath", "Dash cooldown -5 / 10 / 15 / 20 / 25%") },
        { "dash_03", ("Veil of Afterimages", "Damage taken for 1 second after a dash -6 / 12 / 18 / 24 / 30%") },
        { "dash_04", ("Mark of Breakthrough", "Dashing through enemies deals 20 / 40 / 60% of basic attack damage.") },
        { "dash_05", ("Counter Step", "The first attack within 2 seconds after a dash deals +20 / 30 / 40% damage.") },
        { "shield_01", ("Thick Ward", "Maximum shield charges +1 / 2 / 3") },
        { "shield_02", ("Sigil of Endurance", "Invincibility after blocking +0.05 / 0.10 / 0.15 / 0.20 / 0.25s") },
        { "shield_03", ("Sigil of Recharge", "Shield recharge time -5 / 10 / 15%") },
        { "shield_04", ("Ring of Rupture", "When the last charge breaks, knocks back nearby enemies. Range 2 / 2.5 / 3, distance 1 / 1.5 / 2") },
        { "shield_05", ("Guardian's Stride", "Move speed +5 / 10 / 15% while a shield charge is held.") }
    };

    private static readonly Dictionary<string, string> s_resources = new()
    {
        { "prima_materia", "Prima Materia" },
        { "azoth", "Azoth" },
        { "mercury", "Mercury" },
        { "aether_crystal", "Aether Crystal" }
    };

    public const string ComingSoonName = "Coming Soon";
    public const string ComingSoonDescription = "This growth node is not available yet.";

    public static bool TryGetSkill(string skillId, out string name, out string description)
    {
        return TryGet(s_skills, skillId, out name, out description);
    }

    public static bool TryGetNode(string nodeId, out string name, out string description)
    {
        return TryGet(s_nodes, nodeId, out name, out description);
    }

    public static string GetResourceName(string resourceId)
    {
        if (!string.IsNullOrEmpty(resourceId) && s_resources.TryGetValue(resourceId, out string name))
        {
            return name;
        }

        return resourceId;
    }

    private static bool TryGet(Dictionary<string, (string Name, string Description)> table, string id,
        out string name, out string description)
    {
        if (!string.IsNullOrEmpty(id) && table.TryGetValue(id, out (string Name, string Description) text))
        {
            name = text.Name;
            description = text.Description;
            return true;
        }

        name = null;
        description = null;
        return false;
    }
}
