using System;
using UnityEngine;

/// <summary>적이 지정한 수(단계의 ConditionValue, 최소 1)만큼 죽으면 완료한다. 누가 죽였는지는 구분하지 않는다.</summary>
public class EnemyKillCondition : ITutorialCondition
{
    private int _targetCount;
    private int _killCount;
    private bool _isActive;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _targetCount = Mathf.Max(1, Mathf.RoundToInt(step.ConditionValue));
        _killCount = 0;
        _isActive = true;
        EnemyHealth.OnDeath += HandleEnemyDeath;
    }

    public void Tick()
    {
    }

    public void End()
    {
        EnemyHealth.OnDeath -= HandleEnemyDeath;
        _isActive = false;
    }

    private void HandleEnemyDeath(EnemyHealth enemy)
    {
        if (!_isActive)
            return;

        _killCount++;
        if (_killCount < _targetCount)
            return;

        _isActive = false;
        Completed?.Invoke();
    }
}
