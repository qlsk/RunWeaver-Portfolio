using System;

public class StatueInteractCondition : ITutorialCondition
{
    private ExpeditionStatue _statue;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _statue = UnityEngine.Object.FindAnyObjectByType<ExpeditionStatue>();
        _statue.Interacted += HandleInteracted;
    }

    public void Tick() { }

    public void End()
    {
        if (_statue != null)
            _statue.Interacted -= HandleInteracted;

        _statue = null;
    }

    private void HandleInteracted()
    {
        Completed?.Invoke();
    }
}