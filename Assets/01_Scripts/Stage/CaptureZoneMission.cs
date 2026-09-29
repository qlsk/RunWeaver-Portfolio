using UnityEngine;

/// <summary>
/// 미션 5: 거점 점령 (일정 구역을 플레이어가 계속 밟고 있으면 게이지가 차고 100% 달성 시 클리어)
/// </summary>
public class CaptureZoneMission : MissionBase
{
    [Header("점령 설정")]
    [Tooltip("점령 완료(100%)에 필요한 구역 내 총 체류 시간 (초)")]
    [SerializeField] private float targetCaptureTime = 10f;

    [Tooltip("구역 밖으로 벗어났을 때 점령 게이지가 서서히 감소할지 여부")]
    [SerializeField] private bool decayWhenOutside = false;

    [Tooltip("구역 밖일 때 게이지 감소 속도 비율 (1이면 채우는 속도와 동일)")]
    [SerializeField] private float decayRate = 0.5f;

    [Header("점령 구역 (비워두면 씬의 CaptureZone 자동 탐색)")]
    [SerializeField] private CaptureZone targetZone;

    private float currentCaptureTime = 0f;
    private bool isPlayerInZone = false;
    private float lastLogTime = 0f;

    public override void Initialize()
    {
        currentCaptureTime = 0f;
        isPlayerInZone = false;
        lastLogTime = 0f;
        IsCompleted = false;
        IsFailed = false;

        // 점령 구역 자동 탐색
        if (targetZone == null)
        {
            targetZone = FindAnyObjectByType<CaptureZone>();
        }

        if (targetZone != null)
        {
            targetZone.OnPlayerPresenceChanged += HandlePlayerPresenceChanged;
            isPlayerInZone = targetZone.IsPlayerInside;
            Debug.Log($"[CaptureZoneMission] 점령 구역 연결 완료: {targetZone.name}, 목표 시간 {targetCaptureTime}초");
        }
        else
        {
            Debug.LogWarning("[CaptureZoneMission] 씬에 CaptureZone 오브젝트가 없습니다! 점령 구역을 생성해 주세요.");
        }

        NotifyProgressUpdated();
    }

    private void OnDisable()
    {
        if (targetZone != null)
        {
            targetZone.OnPlayerPresenceChanged -= HandlePlayerPresenceChanged;
        }
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

        if (isPlayerInZone)
        {
            currentCaptureTime += deltaTime;
            NotifyProgressUpdated();

            // 1초마다 콘솔에 점령 진행률 로그 출력
            if (Time.time - lastLogTime >= 1f)
            {
                lastLogTime = Time.time;
                int percent = Mathf.FloorToInt(GetProgressRatio() * 100f);
                Debug.Log($"[거점 점령] 점령 진행 중... {percent}% ({currentCaptureTime:F1}초 / {targetCaptureTime:F1}초)");
            }

            if (currentCaptureTime >= targetCaptureTime)
            {
                currentCaptureTime = targetCaptureTime;
                Debug.Log("[거점 점령] ★ 점령 100% 달성! ★");
                CompleteMission(); // 100% 점령 달성!
            }
        }
        else if (decayWhenOutside && currentCaptureTime > 0f)
        {
            currentCaptureTime = Mathf.Max(0f, currentCaptureTime - (deltaTime * decayRate));
            NotifyProgressUpdated();

            if (Time.time - lastLogTime >= 1f)
            {
                lastLogTime = Time.time;
                int percent = Mathf.FloorToInt(GetProgressRatio() * 100f);
                Debug.Log($"[거점 점령] 구역 밖 이탈! 점령도 감소 중... {percent}%");
            }
        }
    }

    private void HandlePlayerPresenceChanged(bool inZone)
    {
        isPlayerInZone = inZone;
        int percent = Mathf.FloorToInt(GetProgressRatio() * 100f);

        if (inZone)
        {
            Debug.Log($"[거점 점령] 플레이어 구역 진입! 점령 시작 (현재 점령률: {percent}%)");
        }
        else
        {
            Debug.Log($"[거점 점령] 플레이어 구역 이탈! 점령 일시정지 (현재 점령률: {percent}%)");
        }

        NotifyProgressUpdated();
    }

    public override float GetProgressRatio()
    {
        if (targetCaptureTime <= 0f) return 1f;
        return Mathf.Clamp01(currentCaptureTime / targetCaptureTime);
    }

    public override string GetProgressText()
    {
        int percentage = Mathf.FloorToInt(GetProgressRatio() * 100f);
        string status = isPlayerInZone ? " (점령 진행 중...)" : " (구역 밖 대기)";
        return $"거점 점령: {percentage}%{status}";
    }
}
