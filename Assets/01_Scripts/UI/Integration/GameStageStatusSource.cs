using UnityEngine;

/// <summary>
/// 스테이지 진행(StageManager, MissionManager)을 심도 패널과 타이머 패널에 연결한다.
/// 스테이지 입장 이벤트가 따로 없어, 새 미션이 시작될 때(OnMissionInitialized)를 입장 시점으로 본다.
/// 스테이지 제한 시간 시스템이 아직 없어 타이머는 "제한 시간 없음"으로 표시한다.
/// </summary>
public class GameStageStatusSource : StageStatusSource
{
    [Tooltip("스테이지 이름 데이터가 생기기 전까지 사용할 이름")]
    [SerializeField] private string _stageName = "Spirit Realm";

    private MissionManager _missionManager;
    private int _maxDepth;

    /// <summary>이번 판에서 도달한 가장 깊은 심도</summary>
    public int MaxDepth => _maxDepth;

    private void Start()
    {
        _missionManager = MissionManager.Instance;
        if (_missionManager == null)
        {
            Debug.LogWarning("[GameStageStatusSource] MissionManager가 없어 스테이지 입장을 알 수 없습니다.", this);
            return;
        }

        _missionManager.OnMissionInitialized += HandleMissionInitialized;
        if (_missionManager.GetCurrentMission() != null)
        {
            HandleMissionInitialized(_missionManager.GetCurrentMission());
        }
    }

    private void OnDestroy()
    {
        if (_missionManager != null)
        {
            _missionManager.OnMissionInitialized -= HandleMissionInitialized;
        }
    }

    private void HandleMissionInitialized(MissionBase mission)
    {
        int depth = StageManager.Instance != null ? StageManager.Instance.CurrentDepth : 1;
        _maxDepth = Mathf.Max(_maxDepth, depth);

        RaiseStageEntered(depth, _stageName);
        RaiseRemainingTimeChanged(NoTimeLimit);
    }
}
