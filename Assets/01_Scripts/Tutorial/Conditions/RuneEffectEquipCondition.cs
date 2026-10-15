using System;
using UnityEngine;

/// <summary>룬북에서 확정한 뒤 단계가 지정한 룬 효과가 장착돼 있으면 완료된다. 슬롯 위치는 묻지 않는다.</summary>
public class RuneEffectEquipCondition : ITutorialCondition
{
    private RuneBookV2Presenter _presenter;
    private string _targetEffectId;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _targetEffectId = step.ConditionTargetId;
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
        RuneEffectLoadout loadout = PlayerRuneManager.Instance.RuneEffectLoadout;
        if (loadout.GetEquippedCount(_targetEffectId) > 0)
            Completed?.Invoke();
    }
}
