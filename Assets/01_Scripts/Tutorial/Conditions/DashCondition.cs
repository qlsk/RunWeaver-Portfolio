using System;
using UnityEngine;

public class DashCondition : ITutorialCondition
{
    private Dash _dash;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _dash = UnityEngine.Object.FindAnyObjectByType<Dash>();
        _dash.DashStarted += HandleDashStarted;
    }

    public void Tick() { }

    public void End()
    {
        if (_dash != null)
            _dash.DashStarted -= HandleDashStarted;

        _dash = null;
    }

    private void HandleDashStarted()
    {
        Completed?.Invoke();
    }
}