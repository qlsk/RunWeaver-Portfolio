using System.Collections.Generic;

/// <summary>
/// 룬 효과 영문 설명 (TMP 폰트에 한글이 없어 DB의 한글 설명 대신 표시한다).
/// 여기에 없는 id는 DB 설명을 그대로 사용한다.
/// </summary>
public static class RuneEffectText
{
    private static readonly Dictionary<string, string> s_descriptions = new()
    {
        { "rune_fire", "Adds the Fire element to bullets." },
        { "rune_water", "Adds the Water element to bullets." },
        { "rune_earth", "Adds the Earth element to bullets." },
        { "rune_wind", "Adds the Wind element to bullets." },
        { "rune_power", "Increases the power of bullets and elemental reactions by 50%." },
        { "rune_speed", "Increases bullet speed by 50%." },
        { "rune_range", "Increases bullet range by 50%." },
        { "rune_grow", "Bullet power gradually increases while traveling, up to 200%." },
        { "rune_duration", "Increases the duration of elemental reaction effects." },
        { "rune_return", "Bullets return to the player after reaching maximum range." },
        { "rune_seek", "Bullets automatically track enemies within a certain range." },
        { "rune_pierce", "Bullets pierce enemies instead of disappearing. Damage drops 5% per pierce (up to 30%)." },
        { "rune_chain", "On hit, the elemental reaction chains once to nearby enemies at 50% damage." },
        { "rune_fix", "At maximum range, bullets stay in place for 10 seconds before disappearing." },
        { "rune_orbit", "At maximum range, bullets orbit the player for 5 seconds." },
        { "rune_spread", "Fires 3 bullets in a fan. Each bullet deals 50% less damage." },
        { "rune_multiple", "Fires bullets twice in a row. Each bullet deals 40% less damage." },
        { "rune_bind", "Binds enemies hit for 1 second, restricting their movement." },
        { "rune_absorb", "Restores health equal to 10% of the damage dealt on hit." },
        { "rune_break", "On hit, destroys enemy shields or applies a weakening effect." },
        { "rune_reverse", "Bullets survive their first hit and reverse direction once." },
        { "rune_echo", "After an elemental reaction occurs, the same reaction triggers once more after a delay." }
    };

    public static string GetDescription(string runeEffectId, string fallback)
    {
        if (!string.IsNullOrEmpty(runeEffectId) && s_descriptions.TryGetValue(runeEffectId, out string description))
        {
            return description;
        }

        return fallback ?? string.Empty;
    }
}
