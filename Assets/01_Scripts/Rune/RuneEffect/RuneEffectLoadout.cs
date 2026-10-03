using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룬 효과의 장착 상태와 활성 슬롯 수를 관리한다.
/// 장착·해제 규칙을 적용하고 룬석을 소비하며,
/// 연구 상태에 따라 활성 슬롯 수를 갱신하고 장착 변경을 알린다.
/// </summary>
public class RuneEffectLoadout
{
    public const int PrimaryElementSlotIndex = 0;
    public const int ElementRuneEffectSlotIndex = 1;

    // 참조 필드
    private readonly RuneEffectDatabase _effectDatabase;
    // private readonly RuneStoneInventory _stoneInventory;
    // private readonly RuneBook _runeBook;

    // 내부 필드
    private readonly List<string> _equippedEffectIds;

    public event Action LoadoutChanged;

    public int ActiveSlotCount { get; }

    public string GetEquippedEffectId(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= ActiveSlotCount)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));

        return slotIndex < _equippedEffectIds.Count ? _equippedEffectIds[slotIndex] : null;
    }

    public RuneEffectLoadout(RuneEffectDatabase runeEffectDatabase)
    {
        _effectDatabase = runeEffectDatabase;
        _equippedEffectIds = new List<string>(5);
        ActiveSlotCount = 2;
    }

    public void Equip(string effectId)
    {
        if (_equippedEffectIds.Count >= ActiveSlotCount)
            return;
        if (!_effectDatabase.TryGetById(effectId, out RuneEffectDefinition effect))
            return;
        if (effect.Category == RuneEffectCategory.Element)
        {
            if (!TryEquipElement(effectId))
                return;
        }
        else
        {
            _equippedEffectIds.Add(effectId);
        }
        LoadoutChanged?.Invoke();
    }

    private bool TryEquipElement(string effectId)
    {
        int slotIndex = PrimaryElementSlotIndex;
        while (slotIndex < _equippedEffectIds.Count)
        {
            if (!_effectDatabase.TryGetById(_equippedEffectIds[slotIndex], out RuneEffectDefinition equippedEffect))
                break;
            if (equippedEffect.Category != RuneEffectCategory.Element)
                break;
            slotIndex++;
        }
        if (slotIndex > ElementRuneEffectSlotIndex)
            return false;
        _equippedEffectIds.Insert(slotIndex, effectId);
        return true;
    }

    public void UnEquip(int slotIndex)
    {
        _equippedEffectIds.RemoveAt(slotIndex);
        LoadoutChanged?.Invoke();
    }

    public void ClearAll()
    {
        _equippedEffectIds.Clear();
        LoadoutChanged?.Invoke();
    }
}
