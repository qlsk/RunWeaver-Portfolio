/// <summary>
/// 한 판이 끝난 사유. 실패 패널 문구와 결과창의 종료 문구를 고르는 데 사용한다.
/// </summary>
public enum RunEndReason
{
    /// <summary>체력이 0이 되어 사망 (You Die)</summary>
    Died,
    /// <summary>미션 실패 (제한 시간 초과, 제단 파괴)</summary>
    MissionFailed,
    /// <summary>일시정지 메뉴에서 탐사 포기</summary>
    Abandoned,
    /// <summary>보스를 처치해 런 클리어</summary>
    Cleared
}
