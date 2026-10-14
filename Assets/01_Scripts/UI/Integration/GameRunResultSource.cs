using UnityEngine;

/// <summary>
/// 실제 게임 데이터로 결과창(EndingUI) 값을 만든다.
/// - 생존 시간: 이 컴포넌트가 시작된 뒤 런이 끝날 때까지 (끝나지 않았으면 결과창을 연 시점까지)
/// - 최고 심도: GameStageStatusSource가 기록한 값
/// - 처치 수, 총 피해량: EnemyDeathTracker
/// - 종료 사유: RunFlow에 기록된 값 (사망 / 미션 실패 / 탐사 포기 / 클리어). 없으면 사망
/// </summary>
public class GameRunResultSource : RunResultSource
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private GameStageStatusSource _stageSource;
    [SerializeField] private EnemyDeathTracker _deathTracker;

    private StageManager _stageManager;
    private float _startTime;
    private float _endTime = -1f;

    private void Start()
    {
        _startTime = Time.time;

        if (_playerHealth != null)
        {
            _playerHealth.Died += StopClock;
        }

        _stageManager = FindAnyObjectByType<StageManager>();
        if (_stageManager != null)
        {
            _stageManager.RunEnded += HandleRunEnded;
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.Died -= StopClock;
        }

        if (_stageManager != null)
        {
            _stageManager.RunEnded -= HandleRunEnded;
        }
    }

    /// <summary>이번 판의 진행 시간(초). 저장용</summary>
    public float ElapsedSeconds => (_endTime >= 0f ? _endTime : Time.time) - _startTime;

    /// <summary>이어하기 : 저장된 진행 시간부터 이어서 센다.</summary>
    public void RestoreElapsed(float seconds)
    {
        _startTime = Time.time - Mathf.Max(0f, seconds);
    }

    public override RunResultData GetResult()
    {
        float endTime = _endTime >= 0f ? _endTime : Time.time;
        int maxDepth = _stageSource != null ? Mathf.Max(1, _stageSource.MaxDepth) : 1;
        int killCount = _deathTracker != null ? _deathTracker.KillCount : 0;
        int totalDamage = _deathTracker != null ? Mathf.RoundToInt(_deathTracker.TotalDamage) : 0;
        RunEndReason endReason = RunFlow.EndReason ?? RunEndReason.Died;

        return new RunResultData(endTime - _startTime, maxDepth, totalDamage, killCount, endReason);
    }

    private void HandleRunEnded(RunEndReason reason)
    {
        StopClock();
    }

    private void StopClock()
    {
        if (_endTime < 0f)
        {
            _endTime = Time.time;
        }
    }
}
