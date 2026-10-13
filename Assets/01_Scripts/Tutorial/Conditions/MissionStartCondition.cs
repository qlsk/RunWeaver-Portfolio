using System;
using UnityEngine;

/// <summary>시작 오브젝트와 상호작용해 미션이 시작되면 완료한다.</summary>
public class MissionStartCondition : ITutorialCondition
{
    private MissionManager _missionManager;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        // MissionManager.Instance는 없으면 새로 만들므로, 생성 없이 기존 인스턴스만 찾는다.
        _missionManager = UnityEngine.Object.FindAnyObjectByType<MissionManager>();
        if (_missionManager == null)
        {
            Debug.LogError("[MissionStartCondition] MissionManager를 찾을 수 없습니다.");
            return;
        }

        _missionManager.OnMissionStarted += HandleMissionStarted;
    }

    public void Tick()
    {
    }

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
