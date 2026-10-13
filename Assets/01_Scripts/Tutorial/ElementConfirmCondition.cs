using System;
using UnityEngine;

public class ElementConfirmCondition : ITutorialCondition
{
    private MainElementSelectView _view;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _view = UnityEngine.Object.FindAnyObjectByType<MainElementSelectView>(FindObjectsInactive.Include);
        if (_view == null)
        {
            Debug.LogError("[ElementConfirmCondition] MainElementSelectView를 찾을 수 없습니다.");
            return;
        }

        _view.Confirmed += HandleConfirmed;
    }

    public void Tick()
    {
    }

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
