using System;
using System.Collections.Generic;

public class RuneLoadout
{
    private const int BasicElementSlotIndex = 0;
    private const int ElementRuneEffectSlotIndex = 1;

    private readonly string[] _equippedEffectIds;
    private int _activeSlotCount;
    private readonly RuneEffectDatabase _effectDatabase;
    private readonly RuneStoneInventory _stoneInventory;
    private readonly RuneBook _runeBook;

    public int ActiveSlotCount => _activeSlotCount;
    public event Action LoadoutChanged;

    public RuneLoadout(RuneEffectDatabase effectDatabase, RuneStoneInventory stoneInventory, RuneBook runeBook)
    {
        _effectDatabase = effectDatabase ?? throw new ArgumentNullException(nameof(effectDatabase));
        _stoneInventory = stoneInventory ?? throw new ArgumentNullException(nameof(stoneInventory));
        _runeBook = runeBook ?? throw new ArgumentNullException(nameof(runeBook));

        _equippedEffectIds = new string[5];
        _activeSlotCount = 2;
    }

    public string GetEquippedEffectId(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _activeSlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex,
                $"Slot index must be between 0 and active slot count {_activeSlotCount}");
        }
        return _equippedEffectIds[slotIndex];
    }

    public IReadOnlyList<string> GetEquippedEffectIds()
    {
        return new List<string>(_equippedEffectIds[.._activeSlotCount]);
    }

    public bool TryEquip(int slotIndex, string effectId)
    {
        if (!_effectDatabase.TryGetById(effectId, out RuneEffectDefinition effect))
        {
            throw new KeyNotFoundException("[RuneLoadout.TryEquip] Effect Id not found");
        }
        if (slotIndex < 0)
        {
            throw new ArgumentException("[RuneLoadout.TryEquip] Slot index cannot be negative");
        }
        if (slotIndex >= _activeSlotCount)
        {
            return false;
        }
        if (!_runeBook.IsRevealed(effectId))
        {
            return false;
        }
        if (_equippedEffectIds[slotIndex] == effectId)
        {
            return false;
        }
        if (Array.Exists(_equippedEffectIds, x => x == effectId) && !effect.CanOverlap)
        {
            return false;
        }
        if (slotIndex <= ElementRuneEffectSlotIndex && effect.Category != RuneEffectCategory.Element)
        {
            throw new InvalidOperationException(
                "[RuneLoadout.TryEquip] Cannot equip a rune effect in the first, second slot if its category is not Element.");
        }
        if (slotIndex > ElementRuneEffectSlotIndex && effect.Category == RuneEffectCategory.Element)
        {
            return false;
        }
        if (!_stoneInventory.ConsumeRuneStones(effect.RuneWord))
        {
            return false;
        }
        _equippedEffectIds[slotIndex] = effectId;
        SafeEventNotifier.Notify(LoadoutChanged);
        return true;
    }

    public bool TryUnequip(int slotIndex)
    {
        if (slotIndex < 0)
        {
            throw new ArgumentException("[RuneLoadout.TryUnequip] Slot index cannot be negative");
        }
        if (slotIndex >= _activeSlotCount)
        {
            throw new InvalidOperationException($"[RuneLoadout.TryUnequip] {slotIndex + 1} slot is not active");
        }
        if (string.IsNullOrWhiteSpace(_equippedEffectIds[slotIndex]))
        {
            throw new InvalidOperationException($"[RuneLoadout.TryUnequip] {slotIndex + 1} slot is already empty");
        }
        _equippedEffectIds[slotIndex] = null;
        SafeEventNotifier.Notify(LoadoutChanged);
        return true;
    }

    public void SetActiveSlotCount(int count)
    {
        if (count < 2 || count > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count,
                "[RuneLoadout.SetActiveSlotCount] count must be between 2 and 5");
        }
        if (count < _activeSlotCount)
        {
            throw new InvalidOperationException(
                $"[RuneLoadout.SetActiveSlotCount] Cannot set count less than current active slotCount {_activeSlotCount}");
        }
        _activeSlotCount = count;
        SafeEventNotifier.Notify(LoadoutChanged);
    }
}