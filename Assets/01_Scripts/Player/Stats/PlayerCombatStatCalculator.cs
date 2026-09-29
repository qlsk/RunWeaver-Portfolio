using System;
using System.Collections.Generic;
using System.Linq;

public class PlayerCombatStatCalculator
{
    private const int CombatStatTypeCount = 4;

    public static PlayerCombatStats Calculate(PlayerCombatStats baseStats, IReadOnlyList<CombatStatModifier> modifiers)
    {
        if (baseStats == null)
        {
            throw new ArgumentNullException(nameof(baseStats));
        }
        if (modifiers == null)
        {
            throw new ArgumentNullException(nameof(modifiers));
        }

        // FinalStat = Clamp((BaseStat + FlatSum) × (1 + PercentSum) × MultiplierProduct, Min, Max)
        // TODO: Clamp 아직 미확정

        var flatSum = new float[CombatStatTypeCount];
        var percentSum = new float[CombatStatTypeCount];
        var multiplierProduct = new float[CombatStatTypeCount];
        Array.Fill(multiplierProduct, 1.0f);
        IOrderedEnumerable<CombatStatModifier> orderedModifiers = modifiers
            .OrderBy(m => m.StatType)
            .ThenBy(m => m.Kind)
            .ThenBy(m => m.Value)
            .ThenBy(m => m.SourceKey, StringComparer.Ordinal);
        foreach (CombatStatModifier modifier in orderedModifiers)
        {
            switch (modifier.Kind)
            {
                case CombatModifierKind.Flat:
                    flatSum[(int)modifier.StatType] += modifier.Value;
                    break;
                case CombatModifierKind.Percent:
                    percentSum[(int)modifier.StatType] += modifier.Value;
                    break;
                case CombatModifierKind.Multiplier:
                    multiplierProduct[(int)modifier.StatType] *= modifier.Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        float maxHealth = (baseStats.MaxHealth + flatSum[(int)CombatStatType.MaxHealth]) *
                          (1 + percentSum[(int)CombatStatType.MaxHealth]) *
                          multiplierProduct[(int)CombatStatType.MaxHealth];
        float moveSpeed = (baseStats.MoveSpeed + flatSum[(int)CombatStatType.MoveSpeed]) *
                          (1 + percentSum[(int)CombatStatType.MoveSpeed]) *
                          multiplierProduct[(int)CombatStatType.MoveSpeed];
        float fireInterval = (baseStats.FireInterval + flatSum[(int)CombatStatType.FireInterval]) *
                             (1 + percentSum[(int)CombatStatType.FireInterval]) *
                             multiplierProduct[(int)CombatStatType.FireInterval];
        float bulletDamage = (baseStats.BulletDamage + flatSum[(int)CombatStatType.BulletDamage]) *
                             (1 + percentSum[(int)CombatStatType.BulletDamage]) *
                             multiplierProduct[(int)CombatStatType.BulletDamage];

        return new PlayerCombatStats(maxHealth, moveSpeed, fireInterval, bulletDamage);
    }
}