using System.Collections.Generic;

public class PowerRuneEffect : RuneEffect
{
    private const float BulletDamagePercent = 0.5f;

    public PowerRuneEffect(string effectId, IReadOnlyCollection<RuneEffectStage> stage) : base(effectId, stage) { }

    public override void Apply(RuneEffectContext context)
    {
        if (context.Stage != RuneEffectStage.Stats)
        {
            return;
        }

        context.AddStatModifier(new CombatStatModifier(
            $"{nameof(PowerRuneEffect)}:{context.SlotIndex}",
            CombatStatType.BulletDamage,
            CombatModifierKind.Percent,
            BulletDamagePercent));
    }
}