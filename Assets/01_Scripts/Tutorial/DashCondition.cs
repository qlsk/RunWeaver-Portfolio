using System;
using UnityEngine;

/// <summary>플레이어가 대시를 한 번 시작하면 완료한다. 대시가 실제로 시작될 때 발생하는 이벤트를 쓰므로 쿨다운 중의 입력은 세지 않는다.</summary>
public class DashCondition : ITutorialCondition
{
    private Dash _dash;

    public event Action Completed;

    public void Begin(TutorialStepSO step)
    {
        _dash = UnityEngine.Object.FindAnyObjectByType<Dash>();
        if (_dash == null)
        {
            Debug.LogError("[DashCondition] Dash를 찾을 수 없습니다.");
            return;
        }

        _dash.DashStarted += HandleDashStarted;
    }

    public void Tick()
    {
    }

    public void End()
    {
        if (_dash != null)
            _dash.DashStarted -= HandleDashStarted;

        _dash = null;
    }

    private void HandleDashStarted()
    {
        Completed?.Invoke();
    }
}
