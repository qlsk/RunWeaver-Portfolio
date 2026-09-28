using System;

[Serializable]
public struct ResearchEffect
{
    public ResearchEffectType Type;
    public string TargetId;
    public float Value;

    public ResearchEffect(ResearchEffectType type, string targetId, float value)
    {
        Type = type;
        TargetId = targetId;
        Value = value;
    }
}
