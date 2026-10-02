using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// 연구 화면에 표시하는 영문 문구 (TMP 폰트에 한글이 없어 DB의 한글 이름 대신 사용한다).
/// 줄(루트) id별 이름, 단계 이름, 설명과 효과 문구를 만든다.
/// </summary>
public static class ResearchText
{
    private class TrackText
    {
        public string Name;
        public string Subtitle;
        public string Description;
        public string[] StepNames;
    }

    private static readonly Dictionary<string, TrackText> s_tracks = new()
    {
        {
            "research_rune_01", new TrackText
            {
                Name = "Rune Resonance",
                Subtitle = "Rune activation limit",
                Description = "Raises the number of rune effects that can be active at the same time.",
                StepNames = new[] { "Novice", "Expanded", "Perfected" }
            }
        },
        {
            "research_rune_02", new TrackText
            {
                Name = "Rune Lexicon",
                Subtitle = "Rune effect categories",
                Description = "Unlocks a new rune effect category so its runes can appear and be equipped.",
                StepNames = new[] { "Behaviour", "Control", "Special" }
            }
        },
        {
            "research_spirit_01", new TrackText
            {
                Name = "Depth Ward",
                Subtitle = "Depth difficulty penalty",
                Description = "Reduces the difficulty penalty of deeper depths.",
                StepNames = new[] { "Adaptation", "Resistance", "Mastery" }
            }
        },
        {
            "research_spirit_02", new TrackText
            {
                Name = "Spirit's Favor",
                Subtitle = "Item drop rate",
                Description = "Increases the chance that enemies drop items.",
                StepNames = new[] { "Attunement", "Guidance", "Blessing" }
            }
        },
        {
            "research_conversion_01", new TrackText
            {
                Name = "Transmutation",
                Subtitle = "Material conversion efficiency",
                Description = "Improves the efficiency of converting one research material into another.",
                StepNames = new[] { "Novice", "Adept", "Master" }
            }
        }
    };

    private static readonly Dictionary<string, string> s_resourceNames = new()
    {
        { "prima_materia", "Prima Materia" },
        { "mercury", "Mercury" },
        { "aether_crystal", "Aether Crystal" },
        { "azoth", "Azoth" }
    };

    private static readonly string[] s_romanNumerals = { "I", "II", "III", "IV", "V" };

    public static string GetTrackName(string rootId, string fallback)
    {
        return s_tracks.TryGetValue(rootId, out TrackText text) ? text.Name : fallback;
    }

    public static string GetTrackSubtitle(string rootId)
    {
        return s_tracks.TryGetValue(rootId, out TrackText text) ? text.Subtitle : string.Empty;
    }

    public static string GetTrackDescription(string rootId, string fallback)
    {
        return s_tracks.TryGetValue(rootId, out TrackText text) ? text.Description : fallback;
    }

    public static string GetStepName(string rootId, int step)
    {
        if (s_tracks.TryGetValue(rootId, out TrackText text) && step >= 1 && step <= text.StepNames.Length)
        {
            return text.StepNames[step - 1];
        }

        return GetRoman(step);
    }

    public static string GetRoman(int step)
    {
        return step >= 1 && step <= s_romanNumerals.Length
            ? s_romanNumerals[step - 1]
            : step.ToString(CultureInfo.InvariantCulture);
    }

    public static string GetResourceName(string resourceId)
    {
        return s_resourceNames.TryGetValue(resourceId, out string name) ? name : resourceId;
    }

    public static string GetCategoryName(ResearchCategory category)
    {
        return category switch
        {
            ResearchCategory.RuneUtilization => "Rune Utilization",
            ResearchCategory.SpiritRealm => "Spirit Realm",
            ResearchCategory.Conversion => "Conversion",
            _ => category.ToString()
        };
    }

    /// <summary>이번 단계 효과 문구. 예: Depth penalty -5%</summary>
    public static string GetStepEffect(ResearchEffect effect)
    {
        return effect.Type switch
        {
            ResearchEffectType.IncreaseRuneActivationLimit => $"Rune activation limit +{FormatNumber(effect.Value)}",
            ResearchEffectType.UnlockRuneEffectCategory => $"Unlock category: {effect.TargetId}",
            ResearchEffectType.ReduceDepthPenalty => $"Depth penalty -{FormatPercent(effect.Value)}",
            ResearchEffectType.IncreaseItemDropRate => $"Item drop rate +{FormatPercent(effect.Value)}",
            ResearchEffectType.IncreaseConversionEfficiency => $"Conversion efficiency +{FormatPercent(effect.Value)}",
            _ => effect.Type.ToString()
        };
    }

    /// <summary>
    /// 줄의 누적 효과 문구. 예: Limit +2, 1 / 3 unlocked
    /// </summary>
    /// <param name="total">연구한 단계 효과 값의 합 (카테고리 해금은 해금한 개수)</param>
    public static string GetTotalEffect(ResearchEffectType type, float total, int stepCount)
    {
        return type switch
        {
            ResearchEffectType.IncreaseRuneActivationLimit => $"Limit +{FormatNumber(total)}",
            ResearchEffectType.UnlockRuneEffectCategory => $"{FormatNumber(total)} / {stepCount} unlocked",
            ResearchEffectType.ReduceDepthPenalty => $"Penalty -{FormatPercent(total)}",
            ResearchEffectType.IncreaseItemDropRate => $"Drop rate +{FormatPercent(total)}",
            ResearchEffectType.IncreaseConversionEfficiency => $"Efficiency +{FormatPercent(total)}",
            _ => FormatNumber(total)
        };
    }

    private static string FormatNumber(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string FormatPercent(float value)
    {
        return (value * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
    }
}
