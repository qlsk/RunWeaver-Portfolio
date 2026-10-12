using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보유 인벤토리 기준 룬석 가중 추첨. EnemyDrop과 RuneStoneDropper가 공유한다.
///
/// - 미보유 문자: 고가중 (기본 5)
/// - 보유 문자: 수량에 반비례해 저가중, 0이 되지는 않는다 (전 종류 수집 차단 없음)
/// - 인벤토리를 못 구하면(미초기화 등) 기존 동작인 균등 랜덤으로 폴백한다.
/// </summary>
public static class RuneStoneDropSelector
{
    public const float DefaultUnownedWeight = 5f;
    public const float DefaultOwnedBaseWeight = 1f;
    public const float DefaultQuantityDampening = 0.5f;

    public static bool TryPick(
        IReadOnlyList<RuneStoneDefinition> defs,
        RuneStoneInventory inventory,
        out RuneStoneDefinition picked)
    {
        return TryPick(
            defs,
            inventory,
            DefaultUnownedWeight,
            DefaultOwnedBaseWeight,
            DefaultQuantityDampening,
            out picked);
    }

    public static bool TryPick(
        IReadOnlyList<RuneStoneDefinition> defs,
        RuneStoneInventory inventory,
        float unownedWeight,
        float ownedBaseWeight,
        float quantityDampening,
        out RuneStoneDefinition picked)
    {
        picked = null;
        if (defs == null || defs.Count == 0) return false;

        float total = 0f;
        for (int i = 0; i < defs.Count; i++)
        {
            RuneStoneDefinition def = defs[i];
            if (def == null) continue;
            total += GetWeight(GetQuantity(inventory, def.RuneCharacter), unownedWeight, ownedBaseWeight, quantityDampening);
        }

        if (total <= 0f)
        {
            return TryPickUniform(defs, out picked);
        }

        float roll = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < defs.Count; i++)
        {
            RuneStoneDefinition def = defs[i];
            if (def == null) continue;
            acc += GetWeight(GetQuantity(inventory, def.RuneCharacter), unownedWeight, ownedBaseWeight, quantityDampening);
            if (roll < acc)
            {
                picked = def;
                return true;
            }
        }

        return TryPickUniform(defs, out picked);
    }

    /// <summary>보유 수량 → 추첨 가중치. 순수 함수라 튜닝 검증에 쓴다.</summary>
    public static float GetWeight(int quantity, float unownedWeight, float ownedBaseWeight, float quantityDampening)
    {
        if (quantity <= 0) return Mathf.Max(0f, unownedWeight);
        float damp = 1f + quantity * Mathf.Max(0f, quantityDampening);
        return Mathf.Max(0f, ownedBaseWeight) / damp;
    }

    private static int GetQuantity(RuneStoneInventory inventory, char runeCharacter)
    {
        return inventory != null ? inventory.GetQuantity(runeCharacter) : 0;
    }

    private static bool TryPickUniform(IReadOnlyList<RuneStoneDefinition> defs, out RuneStoneDefinition picked)
    {
        picked = null;
        if (defs == null || defs.Count == 0) return false;

        RuneStoneDefinition fallback = null;
        for (int i = 0; i < defs.Count; i++)
        {
            if (defs[i] != null)
            {
                fallback = defs[i];
                break;
            }
        }

        if (fallback == null) return false;

        for (int guard = 0; guard < defs.Count; guard++)
        {
            RuneStoneDefinition def = defs[Random.Range(0, defs.Count)];
            if (def != null)
            {
                picked = def;
                return true;
            }
        }

        picked = fallback;
        return true;
    }
}
