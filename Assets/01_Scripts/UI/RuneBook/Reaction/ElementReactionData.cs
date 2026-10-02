/// <summary>
/// 원소 반응 도감의 항목 하나. 기본 원소 효과(원소 1개) 또는 원소 반응(원소 2개)이다.
/// 문구는 모두 영문이다. (TMP 폰트에 한글이 없음)
/// </summary>
public class ElementReactionData
{
    public ElementReactionData(string id, string displayName, ReactionElement elementA, ReactionElement? elementB,
        string description, string shortEffect, string damage = "", string duration = "")
    {
        Id = id;
        DisplayName = displayName;
        ElementA = elementA;
        ElementB = elementB;
        Description = description;
        ShortEffect = shortEffect;
        Damage = damage ?? string.Empty;
        Duration = duration ?? string.Empty;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public ReactionElement ElementA { get; }
    /// <summary>기본 원소 효과면 null</summary>
    public ReactionElement? ElementB { get; }
    public string Description { get; }
    /// <summary>한 줄 요약. 기본 원소 효과에서만 사용 (예: Burn: deals damage over time)</summary>
    public string ShortEffect { get; }
    /// <summary>피해량 문구. 미정이면 빈 문자열</summary>
    public string Damage { get; }
    /// <summary>지속시간 문구. 미정이면 빈 문자열</summary>
    public string Duration { get; }

    public bool IsBaseElement => !ElementB.HasValue;
}
