using System;
using UnityEngine;

/// <summary>시작 석상과 상호작용해 주력 원소 선택 창이 열리면 완료한다.</summary>
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
