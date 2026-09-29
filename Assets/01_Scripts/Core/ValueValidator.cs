using System;

public static class ValueValidator
{
    public static void EnsureFinite(float value, string paramName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be finite.");
        }
    }
}
