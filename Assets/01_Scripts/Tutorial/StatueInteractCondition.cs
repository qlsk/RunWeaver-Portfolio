using System;
using UnityEngine;

public class StatueInteractCondition : ITutorialCondition
{
    private ExpeditionStatue _statue;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _statue = UnityEngine.Object.FindAnyObjectByType<ExpeditionStatue>();
        if (_statue == null)
        {
            Debug.LogError("[StatueInteractCondition] ExpeditionStatue를 찾을 수 없습니다.");
            return;
        }

        _statue.Interacted += HandleInteracted;
    }

    public void Tick()
    {
    }

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
