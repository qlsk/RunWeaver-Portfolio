using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 보유한 룬 문자와 개수.
/// PlayerRuneManager가 만들어서 들고 있으며, 다른 스크립트는 PlayerRuneManager.Instance.RuneStoneInventory로 접근한다.
/// </summary>
public class RuneStoneInventory
{
    private readonly Dictionary<char, RuneStoneItem> _inventory = new();

    public event Action OnInventoryChanged;

    public bool TryAddRuneStone(char runeCharacter, int quantity)
    {
        if (quantity <= 0)
        {
            Debug.LogError("유효하지 않은 획득 수량입니다. 획득 수량은 양수여야 합니다.");
            return false;
        }

        if (!_inventory.ContainsKey(runeCharacter))
        {
            if (!RuneStoneDatabase.Instance.TryGetByRuneCharacter(runeCharacter, out RuneStoneDefinition runeStone))
            {
                Debug.LogError($"{runeCharacter}는 유효하지 않은 룬 문자입니다.");
                return false;
            }

            _inventory.Add(runeCharacter, new RuneStoneItem(runeStone, 0));
        }

        _inventory[runeCharacter].IncreaseQuantity(quantity);
        SafeEventNotifier.Notify(OnInventoryChanged);
        return true;
    }

    public int GetQuantity(char runeCharacter)
    {
        return _inventory.TryGetValue(runeCharacter, out RuneStoneItem item) ? item.Quantity : 0;
    }

    public IReadOnlyList<RuneStoneItem> GetAll()
    {
        return new List<RuneStoneItem>(_inventory.Values);
    }

    public bool HasEnoughRuneStones(string runeWord)
    {
        Dictionary<char, int> requirements = new();
        foreach (char rune in runeWord)
        {
            requirements.TryGetValue(rune, out int quantity);
            requirements[rune] = quantity + 1;
        }

        return HasEnoughRuneStones(requirements);
    }

    public bool HasEnoughRuneStones(IReadOnlyDictionary<char, int> requirements)
    {
        foreach ((char rune, int quantity) in requirements)
        {
            if (GetQuantity(rune) < quantity) return false;
        }

        return true;
    }

    /// <summary>보유한 룬 문자를 모두 비운다. (새 게임)</summary>
    public void Clear()
    {
        if (_inventory.Count == 0) return;

        _inventory.Clear();
        SafeEventNotifier.Notify(OnInventoryChanged);
    }

    public bool ConsumeRuneStones(string runeWord)
    {
        if (!HasEnoughRuneStones(runeWord)) return false;

        foreach (char rune in runeWord)
        {
            _inventory[rune].DecreaseQuantity(1);
        }
        SafeEventNotifier.Notify(OnInventoryChanged);
        return true;
    }
}
