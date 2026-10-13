using System;
using UnityEngine;

/// <summary>임무 보상 선택 창에서 보상을 확정하면 완료한다.</summary>
public class RewardConfirmCondition : ITutorialCondition
{
    private RewardSelectUI _rewardSelectUi;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _rewardSelectUi = UnityEngine.Object.FindAnyObjectByType<RewardSelectUI>(FindObjectsInactive.Include);
        if (_rewardSelectUi == null)
        {
            Debug.LogError("[RewardConfirmCondition] RewardSelectUI를 찾을 수 없습니다.");
            return;
        }

        _rewardSelectUi.OnOptionConfirmed += HandleOptionConfirmed;
    }

    public void Tick()
    {
    }

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
