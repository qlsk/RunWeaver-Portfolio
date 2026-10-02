using System;
using System.Collections.Generic;
using System.Linq;

public class RuneEffectSnapshot
{
    private PlayerCombatStats _stats;
    private IReadOnlyList<RuneEffectSlot> _slots;

    public PlayerCombatStats Stats => _stats;
    public IReadOnlyList<RuneEffectSlot> Slots => _slots;

    public RuneEffectSnapshot(PlayerCombatStats stats, IReadOnlyList<RuneEffectSlot> slots)
    {
        foreach (RuneEffectSlot slot in slots)
        {
            if (slots.Where(other => slot != other).Any(other => slot.SlotIndex == other.SlotIndex))
                throw new ArgumentException($"Slot {slot} is duplicated.");
        }
        _stats = stats;
        _slots = slots.ToList().AsReadOnly();
    }
}