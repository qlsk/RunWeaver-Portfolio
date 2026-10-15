using System;

public interface ITutorialCondition
{
    event Action Completed;

    void Begin(TutorialStepSO step);
    void Tick();
    void End();
}