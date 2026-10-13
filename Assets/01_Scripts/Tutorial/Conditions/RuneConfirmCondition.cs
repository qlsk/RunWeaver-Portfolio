using System;
using UnityEngine;

public class RuneConfirmCondition : ITutorialCondition
{
    private RuneBookV2Presenter _presenter;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _presenter = UnityEngine.Object.FindAnyObjectByType<RuneBookV2Presenter>(FindObjectsInactive.Include);
        _presenter.Confirmed += HandleConfirmed;
    }

    public void Tick() { }

    public void End()
    {
        if (_presenter != null)
            _presenter.Confirmed -= HandleConfirmed;

        _presenter = null;
    }

    private void HandleConfirmed()
    {
        Completed?.Invoke();
    }
}