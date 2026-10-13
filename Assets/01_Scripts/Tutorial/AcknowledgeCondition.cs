using System;
using UnityEngine;

/// <summary>안내를 읽었다는 확인 입력(F)으로 완료한다. 확정 입력은 F로 통일하고 Space는 대시 전용이다.</summary>
public class AcknowledgeCondition : ITutorialCondition
{
    private const KeyCode AcknowledgeKey = KeyCode.F;

    private int _beginFrame;
    private bool _isActive;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _beginFrame = Time.frameCount;
        _isActive = true;
    }

    public void Tick()
    {
        if (!_isActive)
            return;

        // 이전 단계를 끝낸 같은 프레임의 입력이 이어서 이 단계를 닫지 않게 한다.
        if (Time.frameCount <= _beginFrame)
            return;

        if (!Input.GetKeyDown(AcknowledgeKey))
            return;

        _isActive = false;
        Completed?.Invoke();
    }

    public void End()
    {
        _isActive = false;
    }
}
