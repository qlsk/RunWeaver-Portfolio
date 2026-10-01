using UnityEngine;

/// <summary>
/// 미션 1: 적 처치 (일반 몬스터 및 엘리트 몬스터 지정 마릿수 처치)
/// 실제 몬스터(EnemyHealth)와 더미(DummyEnemy) 사망 이벤트를 모두 구독합니다.
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

        // 정적 이벤트 중복 구독 방지: 해제 후 재구독
        UnsubscribeEvents();
        EnemyHealth.OnDeath += HandleEnemyDied;        // 실제 몬스터
        DummyEnemy.OnEnemyDied += HandleDummyEnemyDied;    // 더미 (테스트용)

        NotifyProgressUpdated();
    }

    private void UnsubscribeEvents()
    {
        EnemyHealth.OnDeath -= HandleEnemyDied;
        DummyEnemy.OnEnemyDied -= HandleDummyEnemyDied;
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

    // 실제 몬스터 사망
    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (IsCompleted || IsFailed) return;
        if (enemy == null) return;

        bool isElite = enemy.TryGetComponent(out StageEnemyInfo info) && info.IsElite;
        AddKillCount(isElite, 1);
    }

    // 더미 몬스터 사망
    private void HandleDummyEnemyDied(DummyEnemy enemy)
    {
        if (IsCompleted || IsFailed) return;

        bool isElite = enemy != null && enemy.IsElite;
        AddKillCount(isElite, 1);
    }

    /// <summary>
    /// 처치를 집계하는 공용 메서드. 엘리트 목표가 0이면 엘리트도 일반 처치로 셉니다.
    /// </summary>
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