using System;
using System.Collections.Generic;

public sealed class RuneEffectConfiguration
{
    private const float PowerDamageBonusPerStack = 0.5f;

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
    public float PowerDamageMultiplier => 1 + PowerDamageBonusPerStack * PowerCount;

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
            }
        }

        if ((IsReturn ? 1 : 0) + (IsOrbit ? 1 : 0) + (IsFix ? 1 : 0) > 1)
            throw new InvalidOperationException("Return, Orbit and Fix cannot be equipped together.");
    }
}
