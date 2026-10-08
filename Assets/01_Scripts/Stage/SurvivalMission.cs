using UnityEngine;
using UnityEngine.Serialization;

public class SurvivalMission : MissionBase
{
    public override MissionType Type => MissionType.Survival;

    [Header("생존 미션 설정")]
    [SerializeField, FormerlySerializedAs("survivalDuration")] private float _survivalDuration = 10f; // 목표 생존 시간 (초)

    private float _currentTimer;
    private int _lastLoggedSecond = -1;
    private PlayerHealth _playerHealth;

    public float SurvivalDuration => _survivalDuration;
    public float CurrentTimer => _currentTimer;

    public override void Initialize()
    {
        _currentTimer = 0f;
        _lastLoggedSecond = -1;
        IsCompleted = false;
        IsFailed = false;

        DummyPlayer.OnDummyPlayerDied -= HandlePlayerDied;
        DummyPlayer.OnDummyPlayerDied += HandlePlayerDied;

        // 실제 플레이어(PlayerHealth)가 죽어도 미션 실패로 처리한다.
        UnsubscribePlayerHealth();
        _playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (_playerHealth != null)
        {
            _playerHealth.Died += HandlePlayerDied;
        }

        Debug.Log($"[SurvivalMission] 생존 미션 시작! 목표 시간: {_survivalDuration}초");
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

        _currentTimer += deltaTime;

        // 1초 단위 디버그 로그 출력 (클리어 검증용)
        int currentSecond = Mathf.FloorToInt(_currentTimer);
        if (currentSecond > _lastLoggedSecond && currentSecond <= (int)_survivalDuration)
        {
            _lastLoggedSecond = currentSecond;
            float progressPercent = (_currentTimer / _survivalDuration) * 100f;
            Debug.Log($"[SurvivalMission] 생존 진행 중... {currentSecond}s / {_survivalDuration:F0}s ({progressPercent:F0}%)");
        }

        // 목표 생존 시간 달성 시 클리어
        if (_currentTimer >= _survivalDuration)
        {
            Debug.Log($"[SurvivalMission] ★ 목표 생존 시간({_survivalDuration}초) 달성! 미션 클리어");
            CompleteMission();
        }
    }

    private void HandlePlayerDied()
    {
        if (IsCompleted || IsFailed) return;

        Debug.Log("[SurvivalMission] 플레이어 사망으로 인한 미션 실패!");
        FailMission();
    }

    private void OnDisable()
    {
        DummyPlayer.OnDummyPlayerDied -= HandlePlayerDied;
        UnsubscribePlayerHealth();
    }

    private void UnsubscribePlayerHealth()
    {
        if (_playerHealth != null)
        {
            _playerHealth.Died -= HandlePlayerDied;
            _playerHealth = null;
        }
    }

    public override float GetProgressRatio()
    {
        if (_survivalDuration <= 0f) return 1f;
        return Mathf.Clamp01(_currentTimer / _survivalDuration);
    }

    public override string GetProgressText()
    {
        if (IsFailed) return "플레이어 사망!";
        if (IsCompleted) return "생존 성공!";
        return $"생존 시간: {Mathf.Min(_currentTimer, _survivalDuration):F1}s / {_survivalDuration:F0}s";
    }
}
