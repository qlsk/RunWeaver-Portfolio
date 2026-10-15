using System;

public class RuneEffectSlot
{
    private int _slotIndex;
    private string _effectId;
    private string _sourceKey;
    private RuneEffectExecutionSettings _executionSettings;

    public int SlotIndex => _slotIndex;
    public string EffectId => _effectId;
    public string SourceKey => _sourceKey;
    public RuneEffectExecutionSettings ExecutionSettings => _executionSettings;

    public RuneEffectSlot(int slotIndex,
        string effectId,
        string sourceKey,
        RuneEffectExecutionSettings executionSettings)
    {
        if (slotIndex < 0)
            throw new ArgumentException("Slot index must be greater than or equal to 0.");
        if (string.IsNullOrWhiteSpace(effectId))
            throw new ArgumentException("Effect ID must be specified.");

        _slotIndex = slotIndex;
        _effectId = effectId;
        _sourceKey = sourceKey;
        _executionSettings = executionSettings;
    }
}