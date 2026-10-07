using System;
using UnityEngine;

/// <summary>
/// 스테이지 진행 상태(입장, 남은 제한 시간, 현재 목표와 달성도)를 UI에 알리는 이벤트 창구.
/// 실제 게임은 GameStageStatusSource(StageManager 연결), 테스트는 FakeStageStatusSource를 쓴다.
/// </summary>
public abstract class StageStatusSource : MonoBehaviour
{
    /// <summary>
    /// 남은 시간에 이 값을 넘기면 "제한 시간 없음"으로 처리한다.
    /// </summary>
    public const float NoTimeLimit = -1f;

    /// <summary>
    /// 스테이지에 입장했을 때 발생한다. (심도, 스테이지 이름)
    /// </summary>
    public event Action<int, string> StageEntered;

    /// <summary>
    /// 남은 제한 시간(초)이 바뀌었을 때 발생한다. 제한 시간이 없으면 NoTimeLimit을 전달한다.
    /// </summary>
    public event Action<float> RemainingTimeChanged;

    /// <summary>
    /// 제한 시간이 0이 되었을 때 한 번 발생한다.
    /// </summary>
    public event Action TimeExpired;

    /// <summary>
    /// 현재 목표가 바뀌었을 때 발생한다. (목표 이름, 달성도 문구) 예: ("Survive", "42%")
    /// 타이머 아래에 표시한다.
    /// </summary>
    public event Action<string, string> ObjectiveChanged;

    protected void RaiseStageEntered(int depth, string stageName)
    {
        StageEntered?.Invoke(depth, stageName);
    }

    protected void RaiseRemainingTimeChanged(float remainingSeconds)
    {
        RemainingTimeChanged?.Invoke(remainingSeconds);
    }

    protected void RaiseTimeExpired()
    {
        TimeExpired?.Invoke();
    }

    protected void RaiseObjectiveChanged(string objectiveName, string progressText)
    {
        ObjectiveChanged?.Invoke(objectiveName, progressText);
    }
}
