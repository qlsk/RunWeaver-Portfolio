using UnityEngine;

/// <summary>
/// 미션 4: 단순 생존 (제한시간 동안 웨이브 공격에서 생존)
/// </summary>
public class SurvivalMission : MissionBase
{
    [Header("생존 시간 설정")]
    [SerializeField] private float survivalDuration = 30f; // 30초 생존

    private float currentTimer = 0f;

    public override void Initialize()
    {
        currentTimer = survivalDuration;
        IsCompleted = false;
        IsFailed = false;

        NotifyProgressUpdated();
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

        if (currentTimer > 0f)
        {
            currentTimer -= deltaTime;
            NotifyProgressUpdated();

            if (currentTimer <= 0f)
            {
                currentTimer = 0f;
                CompleteMission(); // 제한시간 종료 시 생존 성공!
            }
        }
    }

    /// <summary>
    /// 플레이어가 사망했을 때 외부(PlayerHealth 등)에서 호출
    /// </summary>
    public void OnPlayerDied()
    {
        if (IsCompleted || IsFailed) return;

        FailMission(); // 플레이어 사망 시 실패!
    }

    public override float GetProgressRatio()
    {
        if (survivalDuration <= 0f) return 1f;
        return Mathf.Clamp01(1f - (currentTimer / survivalDuration));
    }

    public override string GetProgressText()
    {
        return $"생존 남은 시간: {Mathf.CeilToInt(currentTimer)}초";
    }
}
