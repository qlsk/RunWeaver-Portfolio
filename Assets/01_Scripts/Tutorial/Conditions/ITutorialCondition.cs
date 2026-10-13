using System;

/// <summary>튜토리얼 단계 하나의 완료 조건을 감시하는 계약. 단계가 시작되면 Begin, 활성인 동안 매 프레임 Tick, 끝나면 End가 불린다.</summary>
public interface ITutorialCondition
{
    event Action Completed;

    void Begin(TutorialStepSO step);
    void Tick();
    void End();
}