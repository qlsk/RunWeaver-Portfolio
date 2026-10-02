using UnityEngine;

/// <summary>
/// 미션 1: 적 처치 (일반 몬스터 및 엘리트 몬스터 지정 마릿수 처치)
/// EnemyHealth 사망 이벤트를 단일 수신합니다.
/// </summary>
public class EnemyKillMission : MissionBase
{
    [Header("일반 몬스터 목표")]
    [SerializeField] private int targetNormalKillCount = 5;
    private int currentNormalKillCount = 0;

    [Header("엘리트 몬스터 목표 (0이면 엘리트도 일반 처치로 집계)")]
    [SerializeField] private int targetEliteKillCount = 0;
    private int currentEliteKillCount = 0;

    public int CurrentNormalKillCount => currentNormalKillCount;
    public int TargetNormalKillCount => targetNormalKillCount;
    public int CurrentEliteKillCount => currentEliteKillCount;
    public int TargetEliteKillCount => targetEliteKillCount;

    public override void Initialize()
    {
        currentNormalKillCount = 0;
        currentEliteKillCount = 0;
        IsCompleted = false;
        IsFailed = false;

        UnsubscribeEvents();
        EnemyHealth.OnDeath += HandleEnemyDied;

        NotifyProgressUpdated();
    }

    private void UnsubscribeEvents()
    {
        EnemyHealth.OnDeath -= HandleEnemyDied;
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    public override void UpdateMission(float deltaTime)
    {
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (IsCompleted || IsFailed) return;
        if (enemy == null) return;

        bool isElite = false;
        if (enemy.TryGetComponent(out StageEnemyInfo info))
        {
            isElite = info.IsElite;
        }
        else if (enemy.EnemyData != null && enemy.EnemyData.Info != null)
        {
            isElite = enemy.EnemyData.Info.Tier > 1;
        }

        AddKillCount(isElite, 1);
    }

    public void AddKillCount(bool isElite, int amount = 1)
    {
        if (IsCompleted || IsFailed) return;

        if (isElite && targetEliteKillCount <= 0) isElite = false;

        if (isElite)
        {
            currentEliteKillCount += amount;
            Debug.Log($"[적 처치 미션] 엘리트 처치: {currentEliteKillCount} / {targetEliteKillCount}");
        }
        else
        {
            currentNormalKillCount += amount;
            Debug.Log($"[적 처치 미션] 일반 처치: {currentNormalKillCount} / {targetNormalKillCount}");
        }

        NotifyProgressUpdated();

        bool normalClear = currentNormalKillCount >= targetNormalKillCount;
        bool eliteClear = targetEliteKillCount <= 0 || currentEliteKillCount >= targetEliteKillCount;

        if (normalClear && eliteClear)
        {
            CompleteMission();
        }
    }

    public override float GetProgressRatio()
    {
        int totalTarget = targetNormalKillCount + targetEliteKillCount;
        if (totalTarget <= 0) return 1f;

        int totalCurrent = Mathf.Min(currentNormalKillCount, targetNormalKillCount)
            + Mathf.Min(currentEliteKillCount, targetEliteKillCount);

        return Mathf.Clamp01((float)totalCurrent / totalTarget);
    }

    public override string GetProgressText()
    {
        if (targetEliteKillCount > 0)
        {
            return $"적 처치 - 일반: {currentNormalKillCount}/{targetNormalKillCount} | 엘리트: {currentEliteKillCount}/{targetEliteKillCount}";
        }
        return $"적 처치: {currentNormalKillCount}/{targetNormalKillCount}";
    }
}