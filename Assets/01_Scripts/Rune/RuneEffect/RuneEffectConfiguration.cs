using System;
using System.Collections.Generic;

public sealed class RuneEffectConfiguration
{
    public ReactionElement? PrimaryElement { get; }
    public ReactionElement? SecondaryElement { get; }

    public bool IsSpread { get; }
    public bool IsMultiple { get; }
    public bool IsPierce { get; }
    public bool IsReverse { get; }
    public bool IsReturn { get; }
    public bool IsSeek { get; }

    public int BulletsPerShot => IsSpread ? 3 : 1;
    public int ShotsPerAttack => IsMultiple ? 2 : 1;
    public float SpreadDamageMultiplier => IsSpread ? 0.5f : 1f;
    public float MultipleDamageMultiplier => IsMultiple ? 0.6f : 1f;

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
            }
        }
    }
}
