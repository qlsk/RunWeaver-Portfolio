using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 미션 5: 거점 점령 (일정 구역을 플레이어가 계속 밟고 있으면 게이지가 차고 100% 달성 시 클리어)
/// </summary>
public class CaptureZoneMission : MissionBase
{
    [Header("점령 설정")]
    [Tooltip("점령 완료(100%)에 필요한 구역 내 총 체류 시간 (초)")]
    [SerializeField, FormerlySerializedAs("targetCaptureTime")] private float _targetCaptureTime = 10f;

    [Tooltip("구역 밖으로 벗어났을 때 점령 게이지가 서서히 감소할지 여부")]
    [SerializeField, FormerlySerializedAs("decayWhenOutside")] private bool _decayWhenOutside = false;

    [Tooltip("구역 밖일 때 게이지 감소 속도 비율 (1이면 채우는 속도와 동일)")]
    [SerializeField, FormerlySerializedAs("decayRate")] private float _decayRate = 0.5f;

    [Header("점령 구역 (비워두면 씬의 CaptureZone 자동 탐색)")]
    [SerializeField, FormerlySerializedAs("targetZone")] private CaptureZone _targetZone;

    private float _currentCaptureTime = 0f;
    private bool _isPlayerInZone = false;
    private float _lastLogTime = 0f;

    public bool IsPlayerInZone => _isPlayerInZone;

    /// <summary>
    /// MissionManager에서 생성된 신규 점령 구역 오브젝트를 직접 전달받습니다.
    /// </summary>
    public void SetTargetZone(CaptureZone zone)
    {
        _targetZone = zone;
    }

    public override void Initialize()
    {
        _currentCaptureTime = 0f;
        _isPlayerInZone = false;
        _lastLogTime = 0f;
        IsCompleted = false;
        IsFailed = false;

        // 직접 전달받지 않은 경우에만 폴백으로 씬 전체 탐색
        if (_targetZone == null)
        {
            _targetZone = FindAnyObjectByType<CaptureZone>();
        }

        if (_targetZone != null)
        {
            _targetZone.OnPlayerPresenceChanged -= HandlePlayerPresenceChanged;
            _targetZone.OnPlayerPresenceChanged += HandlePlayerPresenceChanged;
            _isPlayerInZone = _targetZone.IsPlayerInside;
            Debug.Log($"[CaptureZoneMission] 점령 구역 연결 완료: {_targetZone.name}, 목표 시간 {_targetCaptureTime}초");
        }
        else
        {
            Debug.LogWarning("[CaptureZoneMission] 씬에 CaptureZone 오브젝트가 없습니다! 점령 구역을 생성해 주세요.");
        }

        NotifyProgressUpdated();
    }

    private void OnDisable()
    {
        UnsubscribeZone();
    }

    private void OnDestroy()
    {
        UnsubscribeZone();
    }

    private void UnsubscribeZone()
    {
        if (_targetZone != null)
        {
            _targetZone.OnPlayerPresenceChanged -= HandlePlayerPresenceChanged;
        }
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

        if (_isPlayerInZone)
        {
            _currentCaptureTime += deltaTime;
            NotifyProgressUpdated();

            if (Time.time - _lastLogTime >= 1f)
            {
                _lastLogTime = Time.time;
                int percent = Mathf.FloorToInt(GetProgressRatio() * 100f);
                Debug.Log($"[거점 점령] 점령 진행 중... {percent}% ({_currentCaptureTime:F1}초 / {_targetCaptureTime:F1}초)");
            }

            if (_currentCaptureTime >= _targetCaptureTime)
            {
                _currentCaptureTime = _targetCaptureTime;
                Debug.Log("[거점 점령] ★ 점령 100% 달성! ★");
                CompleteMission();
            }
        }
        else if (_decayWhenOutside && _currentCaptureTime > 0f)
        {
            _currentCaptureTime = Mathf.Max(0f, _currentCaptureTime - (deltaTime * _decayRate));
            NotifyProgressUpdated();

            if (Time.time - _lastLogTime >= 1f)
            {
                _lastLogTime = Time.time;
                int percent = Mathf.FloorToInt(GetProgressRatio() * 100f);
                Debug.Log($"[거점 점령] 구역 밖 이탈! 점령도 감소 중... {percent}%");
            }
        }
    }

    private void HandlePlayerPresenceChanged(bool inZone)
    {
        _isPlayerInZone = inZone;
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
        if (_targetCaptureTime <= 0f) return 1f;
        return Mathf.Clamp01(_currentCaptureTime / _targetCaptureTime);
    }

    public override string GetProgressText()
    {
        int percentage = Mathf.FloorToInt(GetProgressRatio() * 100f);
        string status = _isPlayerInZone ? " (점령 진행 중...)" : " (구역 밖 대기)";
        return $"거점 점령: {percentage}%{status}";
    }
}
