using System;
using UnityEngine;

/// <summary>포탈을 써서 다음 층이 시작되면 완료한다. Portal.OnPlayerEntered는 가까이 갔을 때 발생하므로 쓰지 않는다.</summary>
public class PortalEnterCondition : ITutorialCondition
{
    private StageManager _stageManager;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        // StageManager.Instance는 없으면 새로 만들거나 예외를 던지므로, 생성 없이 기존 인스턴스만 찾는다.
        _stageManager = UnityEngine.Object.FindAnyObjectByType<StageManager>();
        if (_stageManager == null)
        {
            Debug.LogError("[PortalEnterCondition] StageManager를 찾을 수 없습니다.");
            return;
        }

        _stageManager.StageStarted += HandleStageStarted;
    }

    public void Tick()
    {
    }

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
