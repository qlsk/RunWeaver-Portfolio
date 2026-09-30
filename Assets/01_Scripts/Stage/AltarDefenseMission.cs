using UnityEngine;

/// <summary>
/// 미션 3: 제단 디펜스 (중앙에 생성된 제단이 파괴되지 않도록 방어하며 제한시간 버티기)
/// </summary>
public class AltarDefenseMission : MissionBase
{
    [Header("방어 목표 시간 (초)")]
    [SerializeField] private float defenseDuration = 30f;
    private float currentTimer = 0f;

    [Header("방어 대상 제단 (비워두면 씬의 DefenseAltar 자동 탐색)")]
    [SerializeField] private DefenseAltar targetAltar;

    public override void Initialize()
    {
        currentTimer = defenseDuration;
        IsCompleted = false;
        IsFailed = false;

        // 제단 자동 탐색
        if (targetAltar == null)
        {
            targetAltar = FindAnyObjectByType<DefenseAltar>();
        }

        if (targetAltar == null)
        {
            Debug.LogWarning("[AltarDefenseMission] 씬에 DefenseAltar 오브젝트가 없습니다! 중앙에 제단을 배치해 주세요.");
        }
        else
        {
            // 중복 구독 방지
            targetAltar.OnAltarDestroyed -= HandleAltarDestroyed;
            targetAltar.OnAltarDestroyed += HandleAltarDestroyed;
            targetAltar.OnHealthChanged -= HandleAltarHealthChanged;
            targetAltar.OnHealthChanged += HandleAltarHealthChanged;

            Debug.Log($"[AltarDefenseMission] 제단 방어 미션 시작: 제단 체력 {targetAltar.CurrentHealth}/{targetAltar.MaxHealth}, 목표 시간 {defenseDuration}초");
        }

        NotifyProgressUpdated();
    }

    private void OnDisable()
    {
        UnsubscribeAltarEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeAltarEvents();
    }

    private void UnsubscribeAltarEvents()
    {
        if (targetAltar != null)
        {
            targetAltar.OnAltarDestroyed -= HandleAltarDestroyed;
            targetAltar.OnHealthChanged -= HandleAltarHealthChanged;
        }
    }

    public override void UpdateMission(float deltaTime)
    {
        if (IsCompleted || IsFailed) return;

        if (targetAltar != null && targetAltar.IsDestroyed)
        {
            FailMission();
            return;
        }

        if (currentTimer > 0f)
        {
            currentTimer -= deltaTime;
            NotifyProgressUpdated();

            if (currentTimer <= 0f)
            {
                currentTimer = 0f;
                // 시간이 다 되었을 때 제단이 파괴되지 않았다면 성공!
                if (targetAltar != null && !targetAltar.IsDestroyed)
                {
                    CompleteMission();
                }
            }
        }
    }

    private void HandleAltarDestroyed()
    {
        if (IsCompleted || IsFailed) return;
        FailMission();
    }

    private void HandleAltarHealthChanged(float currentHealth, float maxHealth)
    {
        NotifyProgressUpdated();
    }

    public override float GetProgressRatio()
    {
        // 제단이 파괴되었거나 미션이 실패한 경우 0% 반환 (피드백 반영)
        if (IsFailed || (targetAltar != null && targetAltar.IsDestroyed)) return 0f;
        if (defenseDuration <= 0f) return 1f;

        return Mathf.Clamp01(1f - (currentTimer / defenseDuration));
    }

    public override string GetProgressText()
    {
        if (IsFailed || (targetAltar != null && targetAltar.IsDestroyed))
        {
            return "제단 방어 실패 (제단 파괴됨)";
        }

        string altarInfo = targetAltar != null ? $"제단 HP: {Mathf.CeilToInt(targetAltar.CurrentHealth)}" : "제단 없음";
        return $"제단 방어 남은 시간: {Mathf.CeilToInt(currentTimer)}초 | {altarInfo}";
    }
}
