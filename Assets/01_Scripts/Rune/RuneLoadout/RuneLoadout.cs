using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable All

public class RuneLoadout : IDisposable
{
    private const int BasicElementSlotIndex = 0;
    private const int ElementRuneEffectSlotIndex = 1;

    private readonly string[] _equippedEffectIds;
    private int _activeSlotCount;
    private readonly RuneEffectDatabase _effectDatabase;
    private readonly RuneStoneInventory _stoneInventory;
    private readonly RuneBook _runeBook;
    private readonly ResearchTree _researchTree;
    private IReadOnlyList<ResearchNodeDefinition> _researchDefinitions;
    private bool _disposed;

    public int ActiveSlotCount => _activeSlotCount;
    public event Action LoadoutChanged;

    public RuneLoadout(RuneEffectDatabase effectDatabase,
        RuneStoneInventory stoneInventory,
        RuneBook runeBook,
        ResearchTree researchTree,
        IReadOnlyList<ResearchNodeDefinition> researchDefinitions)
    {
        if (effectDatabase == null)
        {
            throw new ArgumentNullException(nameof(effectDatabase));
        }

        if (stoneInventory == null)
        {
            throw new ArgumentNullException(nameof(stoneInventory));
        }

        if (runeBook == null)
        {
            throw new ArgumentNullException(nameof(runeBook));
        }

        if (researchTree == null)
        {
            throw new ArgumentNullException(nameof(researchTree));
        }

        if (researchDefinitions == null)
        {
            throw new ArgumentNullException(nameof(researchDefinitions));
        }

        int slotIncreaseNodeCount = 0;
        foreach (ResearchNodeDefinition node in researchDefinitions)
        {
            if (node.Effects.Exists(effect => effect.Type == ResearchEffectType.IncreaseRuneActivationLimit))
            {
                slotIncreaseNodeCount++;
            }
        }

        if (slotIncreaseNodeCount > 3)
        {
            throw new ArgumentException("Rune slot research would exceed the 5-slot limit.",
                nameof(researchDefinitions));
        }

        _effectDatabase = effectDatabase;
        _stoneInventory = stoneInventory;
        _runeBook = runeBook;
        _researchTree = researchTree;
        _researchDefinitions = researchDefinitions;

        _equippedEffectIds = new string[5];
        _activeSlotCount = 2;
        RefreshActiveSlotCount();
        _researchTree.NodeActivated += OnResearchNodeActivated;
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
        _runeBook.RevealDetails(effectId);
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

    public void RefreshActiveSlotCount()
    {
        int effectCount = 2;
        foreach (ResearchNodeDefinition node in _researchDefinitions)
        {
            if (!_researchTree.GetNodeIsActivated(node.Id))
            {
                continue;
            }
            if (node.Effects.Exists(x => x.Type == ResearchEffectType.IncreaseRuneActivationLimit))
            {
                effectCount += 1;
            }
        }
        if (effectCount != _activeSlotCount)
        {
            SetActiveSlotCount(effectCount);
        }
    }

    private void OnResearchNodeActivated(string nodeId)
    {
        RefreshActiveSlotCount();
    }

    public void ClearAll()
    {
        if (string.IsNullOrWhiteSpace(_equippedEffectIds[0]))
            return;
        Array.Clear(_equippedEffectIds, 0, _equippedEffectIds.Length);
        SafeEventNotifier.Notify(LoadoutChanged);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _researchTree.NodeActivated -= OnResearchNodeActivated;
        _disposed = true;
    }
}