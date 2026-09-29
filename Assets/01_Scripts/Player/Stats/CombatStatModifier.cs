using System;

public class CombatStatModifier
{
    private readonly string _sourceKey;
    private readonly CombatStatType _statType;
    private readonly CombatModifierKind _kind;
    private readonly float _value;

    public string SourceKey => _sourceKey;
    public CombatStatType StatType => _statType;
    public CombatModifierKind Kind => _kind;
    public float Value => _value;

    public CombatStatModifier(string sourceKey, CombatStatType statType, CombatModifierKind kind, float value)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            throw new ArgumentException("Source key cannot be empty.", nameof(sourceKey));
        }
        if (!Enum.IsDefined(typeof(CombatStatType), statType))
        {
            throw new ArgumentOutOfRangeException(nameof(statType), statType, "Unknown combat stat type.");
        }
        if (!Enum.IsDefined(typeof(CombatModifierKind), kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown combat modifier kind.");
        }
        ValueValidator.EnsureFinite(value, nameof(value));

        _sourceKey = sourceKey;
        _statType = statType;
        _kind = kind;
        _value = value;
    }
}
