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

    // 내부 필드
    private readonly List<string> _equippedEffectIds;

    public IReadOnlyList<string> EquippedEffectIds => _equippedEffectIds;
    public event Action<IReadOnlyList<string>> LoadoutChanged;

    public int ActiveSlotCount { get; private set; }

    /// <summary>보상 특성용: 슬롯 최대치. 기본 2칸에서 시작한다.</summary>
    public const int MaxSlotCount = 5;

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

        if (HasConflictingEndEffect(slotIds, effectId))
        {
            error = EquipError.ConflictingEndEffect;
            return -1;
        }

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

        LoadoutChanged?.Invoke(_equippedEffectIds);
    }

    /// <summary>
    /// 선택 구성을 실제 장착 상태로 확정하고 장착 변경을 알린다.
    /// </summary>
    public void ConfirmEquipment(IReadOnlyList<string> slotIds)
    {
        List<string> confirmed = slotIds.Where(id => !string.IsNullOrEmpty(id)).ToList();
        if (confirmed.Any(id => HasConflictingEndEffect(confirmed, id)))
            throw new InvalidOperationException("Return, Orbit and Fix cannot be equipped together.");

        _equippedEffectIds.Clear();
        _equippedEffectIds.AddRange(confirmed);
        LoadoutChanged?.Invoke(_equippedEffectIds);
    }

    /// <summary>
    /// 보상 특성용: 활성 슬롯을 1칸 늘린다. 최대 MaxSlotCount까지. 성공 시 true.
    /// </summary>
    public bool ExpandSlots()
    {
        if (ActiveSlotCount >= MaxSlotCount)
        {
            return false;
        }

        ActiveSlotCount++;
        LoadoutChanged?.Invoke(_equippedEffectIds);
        return true;
    }

    /// <summary>
    /// 보상 특성용: 활성 슬롯을 기본값(2칸)으로 되돌린다. 런 종료 시 호출.
    /// </summary>
    public void ResetSlots()
    {
        ActiveSlotCount = 2;
        LoadoutChanged?.Invoke(_equippedEffectIds);
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
        LoadoutChanged?.Invoke(_equippedEffectIds);
    }

    /// <summary>
    /// 장착된 모든 룬 효과를 해제하고 장착 변경을 알린다.
    /// </summary>
    public void ClearAll()
    {
        _equippedEffectIds.Clear();
        LoadoutChanged?.Invoke(_equippedEffectIds);
    }

    public static bool HasConflictingEndEffect(IReadOnlyList<string> slotIds, string effectId)
    {
        if (!IsEndEffect(effectId))
            return false;

        return slotIds.Any(id => id != effectId && IsEndEffect(id));
    }

    private static bool IsEndEffect(string effectId)
    {
        return effectId == RuneEffectIds.Return || effectId == RuneEffectIds.Orbit || effectId == RuneEffectIds.Fix;
    }
}