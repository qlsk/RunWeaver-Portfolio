using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실제 게임 데이터(RuneSystemHolder의 RuneBook, RuneLoadout, RuneStoneInventory)를 룬북 UI와 HUD 룬 슬롯에 연결한다.
///
/// CanEquip은 RuneLoadout.TryEquip과 같은 조건을 장착 없이 미리 확인한다.
/// (RuneLoadout에 확인용 메서드가 없어서 같은 규칙을 여기서 확인한다. 장착 규칙이 바뀌면 함께 수정해야 한다)
/// TryEquip / TryUnequip은 RuneLoadout을 그대로 호출한다.
/// </summary>
public class GameRuneBookDataSource : RuneBookDataSource
{
    private const int SlotLimit = 5;
    private const int ElementOnlySlotCount = 2;

    [SerializeField] private RuneSystemHolder _holder;

    private RuneBook _runeBook;
    private RuneLoadout _loadout;
    private RuneStoneInventory _stoneInventory;

    private bool IsBound => _loadout != null;

    public override int MaxSlotCount => SlotLimit;

    public override int ActiveSlotCount => IsBound ? _loadout.ActiveSlotCount : 0;

    private void OnEnable()
    {
        if (_holder == null)
        {
            _holder = FindAnyObjectByType<RuneSystemHolder>();
        }

        if (_holder == null)
        {
            Debug.LogWarning("[GameRuneBookDataSource] RuneSystemHolder가 없어 룬 데이터를 표시하지 않습니다.", this);
            return;
        }

        _holder.Ready += Bind;
        if (_holder.IsReady)
        {
            Bind();
        }
    }

    private void OnDisable()
    {
        if (_holder != null)
        {
            _holder.Ready -= Bind;
        }

        Unbind();
    }

    public override IReadOnlyList<RuneBookEntryData> GetEntries()
    {
        if (!IsBound)
        {
            return Array.Empty<RuneBookEntryData>();
        }

        IReadOnlyList<RuneWordInfo> words = _runeBook.GetAll();
        List<RuneBookEntryData> entries = new(words.Count);
        foreach (RuneWordInfo word in words)
        {
            entries.Add(ToEntry(word));
        }

        return entries;
    }

    public override IReadOnlyList<RuneStoneStackData> GetOwnedRuneStones()
    {
        if (!IsBound)
        {
            return Array.Empty<RuneStoneStackData>();
        }

        List<RuneStoneStackData> stacks = new();
        foreach (RuneStoneItem item in _stoneInventory.GetAll())
        {
            if (item.Quantity <= 0 || item.RuneStoneDefinition == null)
            {
                continue;
            }

            RuneStoneDefinition definition = item.RuneStoneDefinition;
            stacks.Add(new RuneStoneStackData(definition.Id, definition.RuneCharacter, definition.Icon, item.Quantity));
        }

        return stacks;
    }

    public override string GetEquippedEntryId(int slotIndex)
    {
        if (!IsBound || slotIndex < 0 || slotIndex >= _loadout.ActiveSlotCount)
        {
            return null;
        }

        return _loadout.GetEquippedEffectId(slotIndex);
    }

    public override bool CanEquip(int slotIndex, string entryId)
    {
        if (!IsBound || slotIndex < 0 || slotIndex >= _loadout.ActiveSlotCount || string.IsNullOrEmpty(entryId))
        {
            return false;
        }

        RuneEffectDatabase database = RuneEffectDatabase.Instance;
        if (database == null || !database.TryGetById(entryId, out RuneEffectDefinition effect))
        {
            return false;
        }

        IReadOnlyList<string> equippedIds = _loadout.GetEquippedEffectIds();
        if (equippedIds[slotIndex] == entryId)
        {
            return false;
        }

        if (!effect.CanOverlap && Contains(equippedIds, entryId))
        {
            return false;
        }

        // 현재 RuneLoadout 규칙: 1~2번째 칸은 원소 전용, 나머지 칸은 원소 외 전용
        bool isElementSlot = slotIndex < ElementOnlySlotCount;
        bool isElementEffect = effect.Category == RuneEffectCategory.Element;
        if (isElementSlot != isElementEffect)
        {
            return false;
        }

        return _stoneInventory.HasEnoughRuneStones(effect.RuneWord);
    }

    public override bool TryEquip(int slotIndex, string entryId)
    {
        if (!CanEquip(slotIndex, entryId))
        {
            return false;
        }

        try
        {
            return _loadout.TryEquip(slotIndex, entryId);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }
    }

    public override bool CanUnequip(int slotIndex)
    {
        return IsBound && slotIndex >= 0 && slotIndex < _loadout.ActiveSlotCount
               && !string.IsNullOrWhiteSpace(_loadout.GetEquippedEffectId(slotIndex));
    }

    public override bool TryUnequip(int slotIndex)
    {
        if (!CanUnequip(slotIndex))
        {
            return false;
        }

        try
        {
            return _loadout.TryUnequip(slotIndex);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }
    }

    private void Bind()
    {
        Unbind();

        _runeBook = _holder.RuneBook;
        _loadout = _holder.Loadout;
        _stoneInventory = _holder.StoneInventory;
        if (!IsBound)
        {
            return;
        }

        _runeBook.RuneBookChanged += NotifyChanged;
        _loadout.LoadoutChanged += NotifyChanged;
        _stoneInventory.OnInventoryChanged += NotifyChanged;
        NotifyChanged();
    }

    private void Unbind()
    {
        if (_runeBook != null)
        {
            _runeBook.RuneBookChanged -= NotifyChanged;
        }

        if (_loadout != null)
        {
            _loadout.LoadoutChanged -= NotifyChanged;
        }

        if (_stoneInventory != null)
        {
            _stoneInventory.OnInventoryChanged -= NotifyChanged;
        }

        _runeBook = null;
        _loadout = null;
        _stoneInventory = null;
    }

    private static RuneBookEntryData ToEntry(RuneWordInfo word)
    {
        string englishName = word.RuneEffectId;
        RuneEffectDatabase database = RuneEffectDatabase.Instance;
        if (database != null && database.TryGetById(word.RuneEffectId, out RuneEffectDefinition definition)
                             && !string.IsNullOrWhiteSpace(definition.EnglishName))
        {
            englishName = definition.EnglishName;
        }

        return new RuneBookEntryData(word.RuneEffectId, word.RuneWord, englishName,
            RuneEffectText.GetDescription(word.RuneEffectId, word.Description), word.Category, word.RevealLevel,
            word.Icon);
    }

    private static bool Contains(IReadOnlyList<string> ids, string id)
    {
        foreach (string equippedId in ids)
        {
            if (equippedId == id)
            {
                return true;
            }
        }

        return false;
    }
}
