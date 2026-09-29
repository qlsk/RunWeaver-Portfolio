using System;

public class RuneEffectFactory
{
    public RuneEffect Create(string effectId)
    {
        if (!RuneEffectDatabase.Instance.TryGetById(effectId, out RuneEffectDefinition _))
        {
            throw new ArgumentException($"Invalid effect id: {effectId}");
        }

        return effectId switch
        {
            // RuneEffectIds.Power => new PowerRuneEffect(RuneEffectIds.Power, new RuneEffectStage.Stats),
            _ => throw new ArgumentOutOfRangeException(nameof(effectId), effectId, null)
        };
    }
}