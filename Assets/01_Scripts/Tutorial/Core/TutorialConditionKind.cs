/// <summary>튜토리얼 단계의 완료 조건 종류. 선언 순서가 단계 진행 순서와 같다는 전제를 TutorialPreparationWatcher가 쓰므로 순서를 바꾸지 않는다.</summary>
public enum TutorialConditionKind
{
    /// <summary>일정 거리를 이동한다.</summary>
    Move,
    /// <summary>시작 석상과 상호작용한다.</summary>
    StatueInteract,
    /// <summary>주력 원소를 확정한다.</summary>
    ElementConfirm,
    /// <summary>룬 워드 장착을 확정한다.</summary>
    RuneConfirm,
    /// <summary>시작 오브젝트와 상호작용해 임무를 시작한다.</summary>
    MissionStart,
    /// <summary>안내를 읽었다는 확인 입력(F)을 한다.</summary>
    Acknowledge,
    /// <summary>적을 지정한 수만큼 처치한다.</summary>
    EnemyKill,
    /// <summary>대시를 한 번 한다.</summary>
    Dash,
    /// <summary>임무 보상을 확정한다.</summary>
    RewardConfirm,
    /// <summary>포탈을 써서 다음 층으로 내려간다.</summary>
    PortalEnter
}

public static class TutorialConditionKindExtensions
{
    /// <summary>1층이 시작되기 전(시작 석상 대기 중)에 진행되는 조건인지. RuneConfirm은 1층 시작과 겹칠 수 있어 제외한다.</summary>
    public static bool IsPreparationPhase(this TutorialConditionKind kind)
    {
        return kind == TutorialConditionKind.Move
            || kind == TutorialConditionKind.StatueInteract
            || kind == TutorialConditionKind.ElementConfirm;
    }
}
