using System;
using UnityEngine;

public class PortalEnterCondition : ITutorialCondition
{
    private StageManager _stageManager;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _stageManager = UnityEngine.Object.FindAnyObjectByType<StageManager>();
        _stageManager.StageStarted += HandleStageStarted;
    }

    public void Tick() { }

    public void End()
    {
        if (_stageManager != null)
            _stageManager.StageStarted -= HandleStageStarted;

        _stageManager = null;
    }

    private void HandleStageStarted(int depth)
    {
        if (depth >= 2)
            Completed?.Invoke();
    }
}