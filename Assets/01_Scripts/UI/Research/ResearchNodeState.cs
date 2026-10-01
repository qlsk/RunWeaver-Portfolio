/// <summary>
/// 연구 노드의 표시 상태
/// </summary>
public enum ResearchNodeState
{
    /// <summary>선행 연구를 마치지 않음</summary>
    Locked,
    /// <summary>선행 연구는 마쳤지만 재료가 부족함</summary>
    NotEnough,
    /// <summary>지금 연구할 수 있음</summary>
    Available,
    /// <summary>연구 완료</summary>
    Researched
}
