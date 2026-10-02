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
    private const int BasicElementSlotIndex = 0;
    private const int ElementRuneEffectSlotIndex = 1;

    // 참조 필드
    private readonly RuneEffectDatabase _effectDatabase;
    // private readonly RuneStoneInventory _stoneInventory;
    // private readonly RuneBook _runeBook;

    // 내부 필드
    private readonly List<string> _equippedEffectIds;
    private int _activeSlotCount;

    public event Action LoadoutChanged;

    public RuneEffectLoadout(RuneEffectDatabase runeEffectDatabase)
    {
        _effectDatabase = runeEffectDatabase;
        _equippedEffectIds = new List<string>(5);
        _activeSlotCount = 2;
    }

    public void Equip(string effectId)
    {
        if (_equippedEffectIds.Count >= _activeSlotCount)
            return;
        _equippedEffectIds.Add(effectId);
        Debug.Log($"{_equippedEffectIds[0]}, {_equippedEffectIds[1]}");
        LoadoutChanged?.Invoke();
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