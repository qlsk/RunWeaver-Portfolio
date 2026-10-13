public enum TutorialConditionKind
{
    Move,
    StatueInteract,
    ElementConfirm,
    RuneConfirm,
    MissionStart,
    Acknowledge,
    EnemyKill,
    Dash,
    RewardConfirm,
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
