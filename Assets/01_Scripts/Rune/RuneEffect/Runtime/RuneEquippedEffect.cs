public class RuneEquippedEffect
{
    private int _slotIndex;
    private RuneEffectDefinition _definition;
    private RuneEffect _effect;

    public int SlotIndex => _slotIndex;
    public RuneEffectDefinition Definition => _definition;
    public RuneEffect Effect => _effect;

    public RuneEquippedEffect(int slotIndex, RuneEffectDefinition definition, RuneEffect effect)
    {
        _slotIndex = slotIndex;
        _definition = definition;
        _effect = effect;
    }
}