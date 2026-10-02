using System.Collections.Generic;

/// <summary>
/// 연구 비용 한 줄 (재료, 필요량, 보유량)
/// </summary>
public class ResearchCostData
{
    public ResearchCostData(string resourceId, string displayName, int required, int owned)
    {
        ResourceId = resourceId;
        DisplayName = displayName;
        Required = required;
        Owned = owned;
    }

    public string ResourceId { get; }
    public string DisplayName { get; }
    public int Required { get; }
    public int Owned { get; }
    public bool IsEnough => Owned >= Required;
}

/// <summary>
/// 연구 노드 하나를 화면에 표시하기 위한 데이터. 문구는 모두 영문이다.
/// </summary>
public class ResearchNodeData
{
    public string Id { get; set; }
    public string TrackId { get; set; }
    /// <summary>1부터 시작하는 단계</summary>
    public int Step { get; set; }
    public int StepCount { get; set; }
    /// <summary>예: Rune Lexicon II</summary>
    public string DisplayName { get; set; }
    /// <summary>노드 아래에 표시하는 짧은 이름. 예: Control</summary>
    public string StepName { get; set; }
    public string CategoryName { get; set; }
    public string Description { get; set; }
    /// <summary>이번 단계의 효과. 예: Unlock category: Control</summary>
    public string StepEffect { get; set; }
    /// <summary>연구 후 누적 효과. 예: Limit +1 -> +2</summary>
    public string TotalEffect { get; set; }
    /// <summary>선행 연구 이름. 없으면 빈 문자열</summary>
    public string PrerequisiteName { get; set; }
    public bool IsPrerequisiteMet { get; set; }
    public IReadOnlyList<ResearchCostData> Costs { get; set; }
    public ResearchNodeState State { get; set; }
}

/// <summary>
/// 연구 줄 하나 (분류용 루트와 그 아래 단계 노드들)
/// </summary>
public class ResearchTrackData
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Subtitle { get; set; }
    public string CategoryName { get; set; }
    /// <summary>현재까지 연구한 누적 효과. 예: Limit +2</summary>
    public string TotalText { get; set; }
    public IReadOnlyList<string> NodeIds { get; set; }
}
