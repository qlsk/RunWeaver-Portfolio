using System;
using UnityEngine;

/// <summary>
/// 스테이지 진행 상태(입장, 남은 제한 시간)를 UI에 알리는 이벤트 창구.
/// 현재 스테이지 흐름을 관리하는 코드가 없으므로, 생기면 이 클래스를 상속해
/// 실제 이벤트(스테이지 입장, 제한 시간 변경)를 전달하는 어댑터를 만든다.
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
}
