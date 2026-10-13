using System;
using UnityEngine;

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