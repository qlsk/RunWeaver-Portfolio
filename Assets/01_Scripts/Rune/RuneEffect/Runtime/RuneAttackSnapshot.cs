using System.Collections.Generic;

public class RuneAttackSnapshot
{
    private PlayerCombatStats _finalStats;
    private IReadOnlyList<RuneEquippedEffect> _orderedEffects;

    public PlayerCombatStats FinalStats => _finalStats;
    public IReadOnlyList<RuneEquippedEffect> OrderedEffects => _orderedEffects;

    public RuneAttackSnapshot(PlayerCombatStats finalStats, IReadOnlyList<RuneEquippedEffect> orderedEffects)
    {
        _finalStats = finalStats;
        _orderedEffects = orderedEffects;
    }
}