using System;
using UnityEngine;

public class ElementConfirmCondition : ITutorialCondition
{
    private MainElementSelectView _view;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _view = UnityEngine.Object.FindAnyObjectByType<MainElementSelectView>(FindObjectsInactive.Include);
        _view.Confirmed += HandleConfirmed;
    }

    public void Tick() { }

    public void End()
    {
        if (_view != null)
            _view.Confirmed -= HandleConfirmed;
        _view = null;
    }

    private void HandleConfirmed(ReactionElement element)
    {
        Completed?.Invoke();
    }
}