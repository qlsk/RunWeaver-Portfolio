using System;
using UnityEngine;

public class TutorialConditionFactory
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
            // case TutorialConditionKind.RewardConfirm:
            //     break;
            // case TutorialConditionKind.PortalEnter:
            //     break;
            default:
                Debug.LogError("[TutorialConditionFactory] 유효하지 않은 TutorialConditionKind 입니다.");
                return null;
        }
    }
}