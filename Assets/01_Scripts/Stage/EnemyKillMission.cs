using UnityEngine;

/// <summary>
/// 미션 1: 적 처치 (일반 몬스터 및 엘리트 몬스터 지정 마릿수 처치)
/// </summary>
public class EnemyKillMission : MissionBase
{
    [Header("일반 몬스터 목표")]
    [SerializeField] private int targetNormalKillCount = 5;
    private int currentNormalKillCount = 0;

    [Header("엘리트 몬스터 목표 (0이면 일반 몬스터만 처치)")]
    [SerializeField] private int targetEliteKillCount = 0;
    private int currentEliteKillCount = 0;

    public override void Initialize()
    {
        currentNormalKillCount = 0;
        currentEliteKillCount = 0;
        IsCompleted = false;
        IsFailed = false;

        // 정적 이벤트 중복 구독 방지: 기존 등록을 해제한 후 재구독
        DummyEnemy.OnEnemyDied -= HandleEnemyDied;
        DummyEnemy.OnEnemyDied += HandleEnemyDied;

        NotifyProgressUpdated();
    }

    private void OnDisable()
    {
        DummyEnemy.OnEnemyDied -= HandleEnemyDied;
    }

    private void OnDestroy()
    {
        DummyEnemy.OnEnemyDied -= HandleEnemyDied;
    }

    public override void UpdateMission(float deltaTime)
    {
    }

    private void HandleEnemyDied(DummyEnemy enemy)
    {
        if (IsCompleted || IsFailed) return;

        bool isElite = enemy != null && enemy.IsElite;
        AddKillCount(isElite, 1);
    }

    /// <summary>
    /// 외부(실제 EnemyHealth 또는 스포너)에서 처치를 전달받을 수 있는 공용 메서드
    /// </summary>
    public void AddKillCount(bool isElite, int amount = 1)
    {
        if (IsCompleted || IsFailed) return;

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

        // 일반과 엘리트 목표를 모두 달성했는지 검사
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
