using System;

public class RuneWord
{
    private string _runeEffectId;
    private RuneWordRevealLevel _revealLevel;

    public string RuneEffectId => _runeEffectId;
    public RuneWordRevealLevel RevealLevel => _revealLevel;

    public RuneWord(string runeEffectId)
    {
        _runeEffectId = runeEffectId;
        // 모든 룬 워드는 처음부터 목록에 공개된다.
        _revealLevel = RuneWordRevealLevel.Listed;
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
        if ((int)_revealLevel > (int)targetLevel)
        {
            throw new InvalidOperationException($"Cannot lower reveal level: {_runeEffectId} ({_revealLevel} → {targetLevel})");
        }
        _revealLevel = targetLevel;
    }
}
