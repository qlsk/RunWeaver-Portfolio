using System;
using UnityEngine;

public class RewardConfirmCondition : ITutorialCondition
{
    private RewardSelectUI _rewardSelectUi;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _rewardSelectUi = UnityEngine.Object.FindAnyObjectByType<RewardSelectUI>(FindObjectsInactive.Include);
        _rewardSelectUi.OnOptionConfirmed += HandleOptionConfirmed;
    }

    public void Tick() { }

    public void End()
    {
        if (_rewardSelectUi != null)
            _rewardSelectUi.OnOptionConfirmed -= HandleOptionConfirmed;

        _rewardSelectUi = null;
    }

    private void HandleOptionConfirmed(RewardOption option)
    {
        Completed?.Invoke();
    }
}