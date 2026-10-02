using System;

public class RuneWord
{
    private string _runeEffectId;
    private RuneWordRevealLevel _revealLevel;

    public string RuneEffectId => _runeEffectId;
    public RuneWordRevealLevel RevealLevel => _revealLevel;
    public bool IsRevealed => _revealLevel != RuneWordRevealLevel.Hidden;

    public RuneWord(string runeEffectId)
    {
        _runeEffectId = runeEffectId;
        _revealLevel = RuneWordRevealLevel.Hidden;
    }

    public void Reveal(RuneWordRevealLevel targetLevel)
    {
        if (!Enum.IsDefined(typeof(RuneWordRevealLevel), targetLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(targetLevel), targetLevel,
                $"Invalid reveal level for rune: {_runeEffectId}");
        }
        if (_revealLevel == targetLevel)
        {
            throw new InvalidOperationException($"Already at reveal level '{targetLevel}': {_runeEffectId}");
        }
        if (_revealLevel == RuneWordRevealLevel.Hidden && targetLevel == RuneWordRevealLevel.Detailed)
        {
            throw new InvalidOperationException($"List the rune before revealing details: {_runeEffectId}");
        }
        if ((int)_revealLevel > (int)targetLevel)
        {
            throw new InvalidOperationException($"Cannot lower reveal level: {_runeEffectId} ({_revealLevel} → {targetLevel})");
        }
        _revealLevel = targetLevel;
    }
}
