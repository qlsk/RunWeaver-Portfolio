using System;
using UnityEngine;

public class RuneBookOpenCondition : ITutorialCondition
{
    private RuneBookV2Toggle _toggle;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _toggle = UnityEngine.Object.FindAnyObjectByType<RuneBookV2Toggle>(FindObjectsInactive.Include);
        if (_toggle.IsOpen)
        {
            Completed?.Invoke();
            return;
        }

        _toggle.Opened += HandleOpened;
    }

    public void Tick() { }

    public void End()
    {
        if (_toggle != null)
            _toggle.Opened -= HandleOpened;

        _toggle = null;
    }

    private void HandleOpened()
    {
        Completed?.Invoke();
    }
}
