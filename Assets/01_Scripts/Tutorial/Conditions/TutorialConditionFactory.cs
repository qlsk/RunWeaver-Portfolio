using UnityEngine;

/// <summary>완료 조건 종류(TutorialConditionKind)에 맞는 조건 객체를 만든다. 새 조건은 여기에 한 줄을 더한다.</summary>
public static class TutorialConditionFactory
{
    public static ITutorialCondition Create(TutorialConditionKind kind)
    {
        switch (kind)
        {
            case TutorialConditionKind.Move:
                return new MoveCondition();
            case TutorialConditionKind.StatueInteract:
                return new StatueInteractCondition();
            case TutorialConditionKind.ElementConfirm:
                return new ElementConfirmCondition();
            case TutorialConditionKind.RuneConfirm:
                return new RuneConfirmCondition();
            case TutorialConditionKind.MissionStart:
                return new MissionStartCondition();
            case TutorialConditionKind.Acknowledge:
                return new AcknowledgeCondition();
            case TutorialConditionKind.EnemyKill:
                return new EnemyKillCondition();
            case TutorialConditionKind.Dash:
                return new DashCondition();
            case TutorialConditionKind.RewardConfirm:
                return new RewardConfirmCondition();
            case TutorialConditionKind.PortalEnter:
                return new PortalEnterCondition();
            default:
                Debug.LogError("[TutorialConditionFactory] 유효하지 않은 TutorialConditionKind 입니다.");
                return null;
        }
    }
}