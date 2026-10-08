using UnityEngine;

/// <summary>
/// 룬북 UI에 표시할 룬 효과 한 개의 정보
/// </summary>
public sealed class RuneBookEntryData
{
    public string Id { get; }
    public string RuneWord { get; }
    public string EnglishName { get; }
    /// <summary>화면에 보여줄 이름 (한글). 없으면 영문 이름을 쓴다.</summary>
    public string DisplayName { get; }
    public string Description { get; }
    public RuneEffectCategory Category { get; }
    public RuneWordRevealLevel RevealLevel { get; }
    public Sprite Icon { get; }

    public bool AreDetailsRevealed => RevealLevel == RuneWordRevealLevel.Detailed;

    public RuneBookEntryData(string id, string runeWord, string englishName, string description,
        RuneEffectCategory category, RuneWordRevealLevel revealLevel, Sprite icon, string displayName = null)
    {
        Id = id;
        RuneWord = runeWord;
        EnglishName = englishName;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? englishName : displayName;
        Description = description;
        Category = category;
        RevealLevel = revealLevel;
        Icon = icon;
    }
}
