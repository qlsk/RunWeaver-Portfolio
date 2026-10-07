using UnityEngine;

/// <summary>
/// 스테이지 진행(StageManager)을 심도 패널과 타이머 패널에 연결한다.
/// - 층에 들어오면(StageStarted) 심도 패널을 띄운다.
/// - 타이머 : 시작 전에는 미션 제한 시간 전체에서 멈춰 있고, 미션 중에는 남은 시간, 정화 중에는 정화 남은 시간을 보여준다.
///   제한 시간이 없으면(보스 스테이지) "제한 시간 없음"으로 표시한다.
/// - 타이머 아래 : 목표 이름(Survive 등)과 달성도(42%)를 보여준다. 정화 중에는 "Purification"과 "Portal is open"을 보여준다.
/// - 빨간 경고는 시간이 다 되면 실패하는 미션 중에만 켠다. (정화, 생존 미션, 시작 전에는 끈다)
/// - 미션 제한 시간이 0이 되어 미션 실패로 끝나면 TimeExpired를 보낸다.
/// </summary>
public class GameStageStatusSource : StageStatusSource
{
    [Tooltip("스테이지 이름 데이터가 생기기 전까지 사용할 이름")]
    [SerializeField] private string _stageName = "Spirit Realm";

    [Header("목표 문구")]
    [SerializeField] private string _purificationName = "Purification";
    [SerializeField] private string _purificationProgress = "Portal is open";
    [SerializeField] private string _clearedName = "Purified";
    [SerializeField] private string _clearedProgress = "Enter the portal";
    [Tooltip("{0}: 달성도(0~100)")]
    [SerializeField] private string _progressFormat = "{0}%";

    private StageManager _stageManager;
    private int _maxDepth;
    private string _shownName;
    private string _shownProgress;

    /// <summary>이번 판에서 도달한 가장 깊은 심도</summary>
    public int MaxDepth => _maxDepth;

    public override bool IsTimeWarningEnabled =>
        _stageManager != null && _stageManager.Phase == StagePhase.InMission &&
        _stageManager.CurrentMission is not SurvivalMission;

    private void Start()
    {
        _stageManager = FindAnyObjectByType<StageManager>();
        if (_stageManager == null)
        {
            Debug.LogWarning("[GameStageStatusSource] StageManager가 없어 스테이지 진행을 표시할 수 없습니다.", this);
            return;
        }

        _stageManager.StageStarted += HandleStageStarted;
        _stageManager.RunEnded += HandleRunEnded;

        // StageManager.Start가 먼저 실행되어 이미 층이 시작되었으면 바로 표시한다.
        if (_stageManager.CurrentMission != null || _stageManager.IsBossStage)
        {
            HandleStageStarted(_stageManager.CurrentDepth);
        }
    }

    private void OnDestroy()
    {
        if (_stageManager != null)
        {
            _stageManager.StageStarted -= HandleStageStarted;
            _stageManager.RunEnded -= HandleRunEnded;
        }
    }

    private void Update()
    {
        if (_stageManager == null || _stageManager.Phase == StagePhase.Ended)
        {
            return;
        }

        RaiseRemainingTimeChanged(GetDisplayedTime());
        RefreshObjective();
    }

    private void HandleStageStarted(int depth)
    {
        _maxDepth = Mathf.Max(_maxDepth, depth);
        RaiseStageEntered(depth, _stageName);
        RaiseRemainingTimeChanged(GetDisplayedTime());
        _shownName = null;
        RefreshObjective();
    }

    private void HandleRunEnded(RunEndReason reason)
    {
        if (reason == RunEndReason.MissionFailed && _stageManager.RemainingTime == 0f)
        {
            RaiseRemainingTimeChanged(0f);
            RaiseTimeExpired();
        }
    }

    private float GetDisplayedTime()
    {
        return _stageManager.Phase switch
        {
            StagePhase.Cleared => NoTimeLimit,
            StagePhase.Shop => NoTimeLimit,
            _ => _stageManager.RemainingTime < 0f ? NoTimeLimit : _stageManager.RemainingTime
        };
    }

    private void RefreshObjective()
    {
        string objectiveName;
        string progress;

        switch (_stageManager.Phase)
        {
            case StagePhase.Purification:
                objectiveName = _purificationName;
                progress = _purificationProgress;
                break;
            case StagePhase.Cleared:
            case StagePhase.Shop:
                objectiveName = _clearedName;
                progress = _clearedProgress;
                break;
            default:
                objectiveName = _stageManager.ObjectiveName;
                progress = string.Format(_progressFormat, Mathf.FloorToInt(_stageManager.ObjectiveProgress * 100f));
                break;
        }

        if (objectiveName == _shownName && progress == _shownProgress)
        {
            return;
        }

        _shownName = objectiveName;
        _shownProgress = progress;
        RaiseObjectiveChanged(objectiveName, progress);
    }
}
