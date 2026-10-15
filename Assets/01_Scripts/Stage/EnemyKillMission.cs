using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 미션 1: 적 처치 (일반 몬스터 및 엘리트 몬스터 지정 마릿수 처치)
/// EnemyHealth 사망 이벤트를 수신하여 MonsterType(sepcial) 및 Tier 정보 기준으로 엘리트를 판정합니다.
/// </summary>
public class EnemyKillMission : MissionBase
{
    [Header("일반 몬스터 목표")]
    [SerializeField, FormerlySerializedAs("targetNormalKillCount")] private int _targetNormalKillCount = 5;
    private int _currentNormalKillCount = 0;

    [Header("엘리트 몬스터 목표 (0이면 엘리트도 일반 처치로 집계)")]
    [SerializeField, FormerlySerializedAs("targetEliteKillCount")] private int _targetEliteKillCount = 0;
    private int _currentEliteKillCount = 0;

    public int CurrentNormalKillCount => _currentNormalKillCount;
    public int TargetNormalKillCount => _targetNormalKillCount;
    public int CurrentEliteKillCount => _currentEliteKillCount;
    public int TargetEliteKillCount => _targetEliteKillCount;

    public override void Initialize()
    {
        _currentNormalKillCount = 0;
        _currentEliteKillCount = 0;
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

        // 1. StageEnemyInfo 어댑터 컴포넌트 우선 확인
        if (enemy.TryGetComponent(out StageEnemyInfo info))
        {
            isElite = info.IsElite;
        }
        // 2. EnemyDataSO의 MonsterType 및 Tier 정보 확인 (MonsterType.sepcial 오타 호환)
        else if (enemy.EnemyData != null && enemy.EnemyData.Info != null)
        {
            isElite = (enemy.EnemyData.Info.MonsterType == MonsterType.special) || (enemy.EnemyData.Info.Tier > 1);
        }

        AddKillCount(isElite, 1);
    }

    public void AddKillCount(bool isElite, int amount = 1)
    {
        if (IsCompleted || IsFailed) return;

        if (isElite && _targetEliteKillCount <= 0) isElite = false;

        if (isElite)
        {
            _currentEliteKillCount += amount;
            Debug.Log($"[적 처치 미션] ★ 엘리트 처치: {_currentEliteKillCount} / {_targetEliteKillCount}");
        }
        else
        {
            _currentNormalKillCount += amount;
            Debug.Log($"[적 처치 미션] 일반 처치: {_currentNormalKillCount} / {_targetNormalKillCount}");
        }

        NotifyProgressUpdated();

        bool normalClear = _currentNormalKillCount >= _targetNormalKillCount;
        bool eliteClear = _targetEliteKillCount <= 0 || _currentEliteKillCount >= _targetEliteKillCount;

        if (normalClear && eliteClear)
        {
            CompleteMission();
        }
    }

    public override float GetProgressRatio()
    {
        int totalTarget = _targetNormalKillCount + _targetEliteKillCount;
        if (totalTarget <= 0) return 1f;

        int totalCurrent = Mathf.Min(_currentNormalKillCount, _targetNormalKillCount)
            + Mathf.Min(_currentEliteKillCount, _targetEliteKillCount);

        return Mathf.Clamp01((float)totalCurrent / totalTarget);
    }

    public override string GetProgressText()
    {
        if (_targetEliteKillCount > 0)
        {
            return $"적 처치 - 일반: {_currentNormalKillCount}/{_targetNormalKillCount} | 엘리트: {_currentEliteKillCount}/{_targetEliteKillCount}";
        }
        return $"적 처치: {_currentNormalKillCount}/{_targetNormalKillCount}";
    }
}
