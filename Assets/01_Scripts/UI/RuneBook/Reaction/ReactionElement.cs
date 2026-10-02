using UnityEngine;

/// <summary>
/// 원소 반응 도감에서 쓰는 원소 4종
/// </summary>
public enum ReactionElement
{
    Fire,
    Water,
    Earth,
    Wind
}

/// <summary>
/// 원소별 표시 색과 이름
/// </summary>
public static class ReactionElementStyle
{
    public static Color GetColor(ReactionElement element)
    {
        return element switch
        {
            ReactionElement.Fire => new Color(0.816f, 0.341f, 0.227f, 1f),
            ReactionElement.Water => new Color(0.247f, 0.498f, 0.753f, 1f),
            ReactionElement.Earth => new Color(0.588f, 0.439f, 0.243f, 1f),
            _ => new Color(0.314f, 0.627f, 0.471f, 1f)
        };
    }

    public static string GetName(ReactionElement element)
    {
        return element.ToString();
    }
}
