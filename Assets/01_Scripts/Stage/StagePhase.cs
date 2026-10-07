/// <summary>
/// 한 스테이지(층)의 진행 단계.
/// Ready → (시작 오브젝트 F) → InMission → (클리어) → Purification → Cleared → (포탈) → 다음 층의 Ready
/// </summary>
public enum StagePhase
{
    /// <summary>미션이 정해지고 시작 오브젝트 상호작용(F)을 기다리는 중. 적이 나오지 않고 타이머가 멈춰 있다.</summary>
    Ready,
    /// <summary>미션 진행 중. 적이 나오고 미션 타이머가 줄어든다.</summary>
    InMission,
    /// <summary>미션 클리어 후 정화 대기. 스폰과 미션 타이머가 멈추고 정화 카운트다운이 줄어든다. 포탈을 쓸 수 있다.</summary>
    Purification,
    /// <summary>정화가 끝나 남은 적이 사라진 뒤 포탈 사용을 기다리는 중</summary>
    Cleared,
    /// <summary>상점이 열려 있는 중. 상점을 나오면 다음 층을 시작한다.</summary>
    Shop,
    /// <summary>런이 끝났다. (사망, 미션 실패, 탐사 포기, 보스 처치)</summary>
    Ended
}
