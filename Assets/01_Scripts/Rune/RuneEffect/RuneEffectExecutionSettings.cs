public sealed class RuneEffectExecutionSettings
{
    private string _effectId;

    public string EffectId => _effectId;

    public RuneEffectExecutionSettings(string effectId)
    {
        _effectId = effectId;
    }
}