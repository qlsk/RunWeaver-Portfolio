using UnityEngine;

/// <summary>
/// 룬북 UI에 표시할 룬 효과 한 개의 정보
/// </summary>
public sealed class RuneBookEntryData
{
    public string Id { get; }
    public string RuneWord { get; }
    public string EnglishName { get; }
    public string Description { get; }
    public RuneEffectCategory Category { get; }
    public RuneWordRevealLevel RevealLevel { get; }
    public Sprite Icon { get; }

    public bool IsRevealed => RevealLevel != RuneWordRevealLevel.Hidden;
    public bool AreDetailsRevealed => RevealLevel == RuneWordRevealLevel.Detailed;

    public RuneBookEntryData(string id, string runeWord, string englishName, string description,
        RuneEffectCategory category, RuneWordRevealLevel revealLevel, Sprite icon)
    {
        Id = id;
        RuneWord = runeWord;
        EnglishName = englishName;
        Description = description;
        Category = category;
        RevealLevel = revealLevel;
        Icon = icon;
    }
}
