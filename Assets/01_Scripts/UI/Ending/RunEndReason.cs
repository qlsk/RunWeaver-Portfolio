/// <summary>
/// 한 판이 끝난 사유. 결과창의 종료 문구를 고르는 데 사용한다.
/// </summary>
public enum RunEndReason
{
    /// <summary>사망</summary>
    Died,
    /// <summary>안전하게 귀환</summary>
    Returned,
    /// <summary>급하게 탈출</summary>
    Escaped,
    /// <summary>제한 시간 초과</summary>
    TimeOver
}
