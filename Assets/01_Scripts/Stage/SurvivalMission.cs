using UnityEngine;

/// <summary>
/// 미션 4: 단순 생존 (제한시간 동안 수많은 적들의 공세 속에서 생존하기)
/// </summary>
public class SurvivalMission : MissionBase
{
    [Header("생존 목표 시간 (초)")]
    [SerializeField] private float survivalDuration = 30f;
    private float currentTimer = 0f;

    [Header("테스트 편의 기능")]
    [Tooltip("에디터 테스트 시 이 키를 누르면 플레이어가 즉시 사망하여 실패 처리됩니다.")]
    [SerializeField] private KeyCode testFailKey = KeyCode.L;

    public override void Initialize()
    {
        currentTimer = survivalDuration;
        IsCompleted = false;
        IsFailed = false;

        // 더미 플레이어 사망 이벤트 구독
        DummyPlayer.OnDummyPlayerDied += HandlePlayerDied;

        NotifyProgressUpdated();
        Debug.Log($"[SurvivalMission] 생존 미션 개시: 목표 생존 시간 {survivalDuration}초");
    }

    private void OnDisable()
    {
        DummyPlayer.OnDummyPlayerDied -= HandlePlayerDied;
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

        // 테스트 편의: L키 입력 시 실패 처리
        if (Input.GetKeyDown(testFailKey))
        {
            Debug.Log($"[SurvivalMission] 단축키({testFailKey}) 입력으로 플레이어 사망 및 미션 실패 처리!");
            HandlePlayerDied();
            return;
        }

        if (currentTimer > 0f)
        {
            currentTimer -= deltaTime;
            NotifyProgressUpdated();

            if (currentTimer <= 0f)
            {
                currentTimer = 0f;
                CompleteMission(); // 제한시간 만료 시 생존 성공!
            }
        }
    }

    /// <summary>
    /// 외부(실제 PlayerHealth 또는 더미)에서 플레이어 사망 시 호출
    /// </summary>
    public void HandlePlayerDied()
    {
        if (IsCompleted || IsFailed) return;

        Debug.LogWarning("[SurvivalMission] ✖ 플레이어가 사망하여 미션에 실패했습니다! ✖");
        FailMission();
    }

    public override float GetProgressRatio()
    {
        if (survivalDuration <= 0f) return 1f;
        return Mathf.Clamp01(1f - (currentTimer / survivalDuration));
    }

    public override string GetProgressText()
    {
        return $"생존 남은 시간: {Mathf.CeilToInt(currentTimer)}초 (생존 중)";
    }
}
