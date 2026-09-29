using UnityEngine;

/// <summary>
/// 미션 5: 거점 점령 (지정 구역 내 체류하여 점령 게이지 100% 달성)
/// </summary>
public class CaptureZoneMission : MissionBase
{
    [Header("점령 설정")]
    [Tooltip("점령 완수에 필요한 총 체류 시간 (초)")]
    [SerializeField] private float targetCaptureTime = 15f; 
    [Tooltip("구역 이탈 시 점령 게이지 감소 여부")]
    [SerializeField] private bool decayWhenOutside = false;
    [Tooltip("구역 이탈 시 감소 속도 비율")]
    [SerializeField] private float decayRate = 0.5f;

    private float currentCaptureTime = 0f;
    private bool isPlayerInZone = false;

    public override void Initialize()
    {
        currentCaptureTime = 0f;
        isPlayerInZone = false;
        IsCompleted = false;
        IsFailed = false;

        NotifyProgressUpdated();
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

        if (isPlayerInZone)
        {
            currentCaptureTime += deltaTime;
            if (currentCaptureTime >= targetCaptureTime)
            {
                currentCaptureTime = targetCaptureTime;
                CompleteMission();
            }
            NotifyProgressUpdated();
        }
        else if (decayWhenOutside && currentCaptureTime > 0f)
        {
            currentCaptureTime -= deltaTime * decayRate;
            if (currentCaptureTime < 0f) currentCaptureTime = 0f;
            NotifyProgressUpdated();
        }
    }

    /// <summary>
    /// 플레이어가 점령 영역 진입/이탈 시 거점 트리거 영역에서 호출
    /// </summary>
    public void SetPlayerInZone(bool inZone)
    {
        isPlayerInZone = inZone;
        Debug.Log($"[CaptureZoneMission] 플레이어 점령 영역 상태: {(inZone ? "진입" : "이탈")}");
    }

    public override float GetProgressRatio()
    {
        if (targetCaptureTime <= 0f) return 1f;
        return Mathf.Clamp01(currentCaptureTime / targetCaptureTime);
    }

    public override string GetProgressText()
    {
        int percentage = Mathf.FloorToInt(GetProgressRatio() * 100f);
        string status = isPlayerInZone ? " (점령 중...)" : " (구역 밖)";
        return $"거점 점령: {percentage}%{status}";
    }
}
