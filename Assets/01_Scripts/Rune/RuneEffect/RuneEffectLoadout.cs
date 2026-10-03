using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 룬 효과의 장착 상태와 활성 슬롯 수를 관리한다.
/// 장착 구성과 룬석 소비 조건을 검사하고, 확정 장착의 변경을 알린다.
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

    /// <summary>
    /// 기본 활성 슬롯 두 칸을 가진 빈 룬 효과 로드아웃을 생성한다.
    /// </summary>
    public RuneEffectLoadout(RuneEffectDatabase runeEffectDatabase)
    {
        _effectDatabase = runeEffectDatabase;
        _equippedEffectIds = new List<string>(5);
        ActiveSlotCount = 2;
    }

    /// <summary>
    /// 확정된 장착 목록에서 슬롯의 룬 효과를 조회한다.
    /// </summary>
    public string GetEquippedEffectId(int slotIndex)
    {
        return slotIndex < _equippedEffectIds.Count ? _equippedEffectIds[slotIndex] : null;
    }

    public int GetEquippedCount(string effectId)
    {
        return _equippedEffectIds.Count(id => id == effectId);
    }

    /// <summary>
    /// 룬 효과의 추가 장착 가능 여부와 장착 규칙에 맞는 삽입 위치를 판단한다.
    /// </summary>
    public int FindEquipSlot(IReadOnlyList<string> slotIds, string effectId, out EquipError error)
    {
        var occupied = 0;
        var elements = 0;
        foreach (string id in slotIds)
        {
            if (string.IsNullOrEmpty(id))
                continue;

            if (!_effectDatabase.TryGetById(id, out RuneEffectDefinition equippedEffect))
                throw new InvalidOperationException($"Unknown rune effect ID: {id}");

            occupied++;

            if (equippedEffect.Category == RuneEffectCategory.Element)
                elements++;
        }

        error = EquipError.None;

        if (!_effectDatabase.TryGetById(effectId, out RuneEffectDefinition effect))
            throw new InvalidOperationException($"Unknown rune effect ID: {effectId}");

        if (slotIds.Any(id => id == effectId && !CanOverlap(effectId)))
        {
            error = EquipError.CannotStack;
            return -1;
        }

        if (effect.Category == RuneEffectCategory.Element && elements > ElementRuneEffectSlotIndex)
            error = EquipError.ElementLimit;
        else if (occupied >= ActiveSlotCount)
            error = EquipError.SlotsFull;

        if (error != EquipError.None)
            return -1;

        return effect.Category == RuneEffectCategory.Element ? elements : occupied;
    }

    /// <summary>
    /// 같은 룬 효과를 여러 슬롯에 장착할 수 있는지 확인한다.
    /// 원소 효과는 중복을 허용하고, 비원소 효과는 개별 중복 허용 설정을 따른다.
    /// </summary>
    public bool CanOverlap(string effectId)
    {
        if (!_effectDatabase.TryGetById(effectId, out RuneEffectDefinition effect))
            throw new InvalidOperationException($"Unknown rune effect ID: {effectId}");

        return effect.Category == RuneEffectCategory.Element || effect.CanOverlap;
    }

    /// <summary>
    /// 주 원소 슬롯의 룬 효과를 설정하거나 교체하고 장착 변경을 알린다.
    /// </summary>
    public void EquipPrimaryElement(string effectId)
    {
        if (!_effectDatabase.TryGetById(effectId, out _))
            throw new InvalidOperationException($"Unknown rune effect ID: {effectId}");

        if (_equippedEffectIds.Count == 0)
            _equippedEffectIds.Insert(PrimaryElementSlotIndex, effectId);
        else
            _equippedEffectIds[PrimaryElementSlotIndex] = effectId;

        LoadoutChanged?.Invoke();
    }

    /// <summary>
    /// 선택 구성을 실제 장착 상태로 확정하고 장착 변경을 알린다.
    /// </summary>
    public void ConfirmEquipment(IReadOnlyList<string> slotIds)
    {
        List<string> confirmed = slotIds.Where(id => !string.IsNullOrEmpty(id)).ToList();
        _equippedEffectIds.Clear();
        _equippedEffectIds.AddRange(confirmed);
        LoadoutChanged?.Invoke();
    }

    /// <summary>
    /// 슬롯의 룬 효과를 해제하고 장착 변경을 알린다.
    /// </summary>
    public void UnEquip(int slotIndex)
    {
        string effectId = _equippedEffectIds[slotIndex];
        if (!_effectDatabase.TryGetById(effectId, out _))
            throw new InvalidOperationException($"Unknown rune effect ID: {effectId}");
        _equippedEffectIds.RemoveAt(slotIndex);
        LoadoutChanged?.Invoke();
    }

    /// <summary>
    /// 장착된 모든 룬 효과를 해제하고 장착 변경을 알린다.
    /// </summary>
    public void ClearAll()
    {
        _equippedEffectIds.Clear();
        LoadoutChanged?.Invoke();
    }
}
