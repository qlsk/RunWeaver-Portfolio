using UnityEngine;

public class SurvivalMission : MissionBase
{
    [Header("생존 목표 시간 (초)")]
    [SerializeField] private float survivalDuration = 10f;

    private float elapsed;

    public float SurvivalDuration => survivalDuration;
    public float Elapsed => elapsed;
    public float Remaining => Mathf.Max(0f, survivalDuration - elapsed);

#if UNITY_EDITOR
    [Header("테스트 편의 (에디터 전용)")]
    [SerializeField] private KeyCode testFailKey = KeyCode.L;
#endif
    private int lastNotifiedSecond = -1;

    public override void Initialize()
    {
        elapsed = 0f;
        IsCompleted = false;
        IsFailed = false;
        lastNotifiedSecond = -1;

        DummyPlayer.OnDummyPlayerDied -= HandlePlayerDied;
        DummyPlayer.OnDummyPlayerDied += HandlePlayerDied;

        NotifyProgressUpdated();
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

#if UNITY_EDITOR
        if (Input.GetKeyDown(testFailKey)) { HandlePlayerDied(); return; }
#endif

        elapsed += deltaTime;

        int sec = Mathf.CeilToInt(Remaining);
        if (sec != lastNotifiedSecond)
        {
            lastNotifiedSecond = sec;
            NotifyProgressUpdated();
        }

        if (elapsed >= survivalDuration)
        {
            elapsed = survivalDuration;
            NotifyProgressUpdated();   // 마지막 100% 상태를 UI에 확실히 전달
            CompleteMission();
        }
    }

    public void HandlePlayerDied()
    {
        if (IsCompleted || IsFailed) return;
        FailMission();
    }

    public override float GetProgressRatio()
    {
        if (IsFailed) return 0f;
        if (survivalDuration <= 0f) return 1f;
        return Mathf.Clamp01(elapsed / survivalDuration);
    }

    public override string GetProgressText()
    {
        if (IsFailed) return "생존 실패 (플레이어 사망)";
        return $"생존 남은 시간: {Mathf.CeilToInt(Remaining)}초";
    }
}