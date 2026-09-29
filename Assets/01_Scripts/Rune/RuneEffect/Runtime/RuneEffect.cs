using System.Collections.Generic;

public abstract class RuneEffect
{
    private string _effectId;
    private IReadOnlyCollection<RuneEffectStage> _stage;

    public string EffectId => _effectId;
    public IReadOnlyCollection<RuneEffectStage> Stage => _stage;

    public RuneEffect(string effectId, IReadOnlyCollection<RuneEffectStage> stage)
    {
        _effectId = effectId;
        _stage = stage;
    }

    public abstract void Apply(RuneEffectContext context);
}