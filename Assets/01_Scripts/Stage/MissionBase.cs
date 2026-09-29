using System;
using UnityEngine;

/// <summary>
/// 5가지 미션 유형(처치, 영혼 수집, 제단 디펜스, 단순 생존, 거점 점령)의 공통 추상 클래스 (Strategy Pattern)
/// </summary>
public abstract class MissionBase : MonoBehaviour
{
    public event Action OnMissionCompleted;
    public event Action OnMissionFailed;
    public event Action<float, string> OnMissionProgressUpdated;

    public bool IsCompleted { get; protected set; }
    public bool IsFailed { get; protected set; }

    /// <summary>
    /// 미션 시작 시 초기화
    /// </summary>
    public abstract void Initialize();

    /// <summary>
    /// 매 프레임 업데이트 (시간 제한, 거점 점령 게이지 연산 등)
    /// </summary>
    public abstract void UpdateMission(float deltaTime);

    /// <summary>
    /// 현재 진행률 (0.0f ~ 1.0f)
    /// </summary>
    public abstract float GetProgressRatio();

    /// <summary>
    /// UI에 표시할 진행 상태 설명 텍스트 (예: "처치 수: 3 / 5")
    /// </summary>
    public abstract string GetProgressText();

    protected void CompleteMission()
    {
        if (IsCompleted || IsFailed) return;

        IsCompleted = true;
        Debug.Log($"[MissionBase] ★ 미션 성공! ({GetType().Name}) ★");
        OnMissionCompleted?.Invoke();
    }

    protected void FailMission()
    {
        if (IsCompleted || IsFailed) return;

        IsFailed = true;
        Debug.LogWarning($"[MissionBase] ✖ 미션 실패! ({GetType().Name}) ✖");
        OnMissionFailed?.Invoke();
    }

    protected void NotifyProgressUpdated()
    {
        OnMissionProgressUpdated?.Invoke(GetProgressRatio(), GetProgressText());
    }
}
