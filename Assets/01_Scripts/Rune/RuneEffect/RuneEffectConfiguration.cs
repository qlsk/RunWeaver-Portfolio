using System;
using System.Collections.Generic;

public sealed class RuneEffectConfiguration
{
    private const float PowerDamageBonusPerStack = 0.5f;
    private const float SpeedBonusPerStack = 0.5f;
    private const float RangeBonusPerStack = 0.5f;

    public const float EchoDelaySeconds = 1f;

    public ReactionElement? PrimaryElement { get; }
    public ReactionElement? SecondaryElement { get; }

    public bool IsSpread { get; }
    public bool IsMultiple { get; }
    public bool IsPierce { get; }
    public bool IsReverse { get; }
    public bool IsReturn { get; }
    public bool IsSeek { get; }
    public bool IsOrbit { get; }
    public bool IsFix { get; }
    public bool IsChain { get; }

    public int BulletsPerShot => IsSpread ? 3 : 1;
    public int ShotsPerAttack => IsMultiple ? 2 : 1;
    public float SpreadDamageMultiplier => IsSpread ? 0.5f : 1f;
    public float MultipleDamageMultiplier => IsMultiple ? 0.6f : 1f;
    public int PowerCount { get; private set; }
    public float PowerDamageMultiplier => 1f + PowerDamageBonusPerStack * PowerCount;
    public int SpeedCount { get; private set; }
    public float SpeedMultiplier => 1f + SpeedBonusPerStack * SpeedCount;
    public int RangeCount { get; private set; }
    public float RangeMultiplier => 1f + RangeBonusPerStack * RangeCount;
    public int EchoCount { get; private set; }

    public RuneEffectConfiguration(IReadOnlyList<RuneEffectDefinition> effects)
    {
        foreach (RuneEffectDefinition effect in effects)
        {
            if (effect.Category == RuneEffectCategory.Element)
            {
                ReactionElement element = Enum.Parse<ReactionElement>(effect.EnglishName, true);
                if (!PrimaryElement.HasValue)
                    PrimaryElement = element;
                else
                    SecondaryElement = element;
            }

            switch (effect.Id)
            {
                case RuneEffectIds.Power:
                    PowerCount += 1;
                    break;
                case RuneEffectIds.Speed:
                    SpeedCount += 1;
                    break;
                case RuneEffectIds.Range:
                    RangeCount += 1;
                    break;
                case RuneEffectIds.Spread:
                    IsSpread = true;
                    break;
                case RuneEffectIds.Multiple:
                    IsMultiple = true;
                    break;
                case RuneEffectIds.Pierce:
                    IsPierce = true;
                    break;
                case RuneEffectIds.Reverse:
                    IsReverse = true;
                    break;
                case RuneEffectIds.Return:
                    IsReturn = true;
                    break;
                case RuneEffectIds.Seek:
                    IsSeek = true;
                    break;
                case RuneEffectIds.Orbit:
                    IsOrbit = true;
                    break;
                case RuneEffectIds.Fix:
                    IsFix = true;
                    break;
                case RuneEffectIds.Chain:
                    IsChain = true;
                    break;
                case RuneEffectIds.Echo:
                    EchoCount += 1;
                    break;
            }
        }

        if ((IsReturn ? 1 : 0) + (IsOrbit ? 1 : 0) + (IsFix ? 1 : 0) > 1)
            throw new InvalidOperationException("Return, Orbit and Fix cannot be equipped together.");
    }
}