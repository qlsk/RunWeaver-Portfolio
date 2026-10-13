using System;
using UnityEngine;

public class MissionStartCondition : ITutorialCondition
{
    private MissionManager _missionManager;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _missionManager = UnityEngine.Object.FindAnyObjectByType<MissionManager>();
        _missionManager.OnMissionStarted += HandleMissionStarted;
    }

    public void Tick() { }

    public void End()
    {
        if (_missionManager != null)
            _missionManager.OnMissionStarted -= HandleMissionStarted;

        _missionManager = null;
    }

    private void HandleMissionStarted(MissionBase mission)
    {
        Completed?.Invoke();
    }
}