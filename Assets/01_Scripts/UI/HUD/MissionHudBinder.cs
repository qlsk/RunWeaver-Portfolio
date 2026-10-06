using UnityEngine;

/// <summary>
/// MissionManager의 현재 미션 진행 상황을 HUD 미션 두루마리에 표시한다.
/// 층마다 새 미션이 시작되므로(OnMissionInitialized) 그때마다 새 미션의 진행 이벤트로 구독을 옮긴다.
/// 미션의 진행 문구는 한글이라 TMP 폰트로 표시할 수 없으므로 MissionHudText로 영문 문구를 만든다.
/// 생존·제단 방어 미션은 남은 시간이 바뀌어도 진행 이벤트를 보내지 않고, 미션 완료/실패 때도 이벤트가 없어
/// 매 프레임 문구를 만들어 바뀌었을 때만 갱신한다.
/// 씬에 MissionManager가 없으면 아무것도 하지 않는다.
///
/// [변경] 미션 두루마리는 사용하지 않는다. 미션 종류와 달성도는 타이머 아래에 표시한다. (TimerPanelPresenter)
/// _isScrollHidden이 켜져 있으면(기본) 시작할 때 두루마리를 숨기고 이 컴포넌트를 끈다.
/// </summary>
[DisallowMultipleComponent]
public class MissionHudBinder : MonoBehaviour
{
    [SerializeField] private MissionScrollView _view;
    [Tooltip("켜져 있으면 미션 두루마리를 숨기고 갱신하지 않는다. (미션 표시는 타이머 아래로 옮김)")]
    [SerializeField] private bool _isScrollHidden = true;

    private MissionManager _missionManager;
    private MissionBase _subscribedMission;
    private string _lastProgressText;
    private MissionBase _lastShownMission;

    private void Awake()
    {
        if (!_isScrollHidden)
        {
            return;
        }

        if (_view != null)
        {
            _view.gameObject.SetActive(false);
        }

        enabled = false;
    }

    private void Start()
    {
        if (_isScrollHidden)
        {
            return;
        }

        // MissionManager.Instance는 MissionManager.Awake에서 설정되므로 Start에서 찾는다.
        _missionManager = MissionManager.Instance;
        if (_missionManager == null || _view == null)
        {
            return;
        }

        _missionManager.OnMissionInitialized += HandleMissionInitialized;

        // MissionManager.Start가 먼저 실행되어 이미 미션이 있으면 바로 연결한다.
        MissionBase currentMission = _missionManager.GetCurrentMission();
        if (currentMission != null)
        {
            HandleMissionInitialized(currentMission);
        }
    }

    private void Update()
    {
        if (_subscribedMission != null)
        {
            RefreshView();
        }
    }

    private void OnDestroy()
    {
        if (_missionManager != null)
        {
            _missionManager.OnMissionInitialized -= HandleMissionInitialized;
        }

        SubscribeMission(null);
    }

    private void HandleMissionInitialized(MissionBase mission)
    {
        SubscribeMission(mission);

        if (mission != null)
        {
            _lastProgressText = null;
            RefreshView();
        }
    }

    private void SubscribeMission(MissionBase mission)
    {
        if (_subscribedMission == mission)
        {
            return;
        }

        if (_subscribedMission != null)
        {
            _subscribedMission.OnMissionProgressUpdated -= HandleMissionProgressUpdated;
        }

        _subscribedMission = mission;

        if (_subscribedMission != null)
        {
            _subscribedMission.OnMissionProgressUpdated += HandleMissionProgressUpdated;
        }
    }

    private void HandleMissionProgressUpdated(float progressRatio, string progressText)
    {
        RefreshView();
    }

    private void RefreshView()
    {
        string progressText = MissionHudText.GetProgressText(_subscribedMission);
        if (progressText == _lastProgressText)
        {
            return;
        }

        // 같은 미션의 남은 시간이나 점령률처럼 계속 바뀌는 값만 바뀐 경우에는 말아 둔 두루마리를 펼치지 않는다.
        bool isTimerTick = _subscribedMission == _lastShownMission && IsContinuouslyUpdating(_subscribedMission);

        _lastProgressText = progressText;
        _lastShownMission = _subscribedMission;
        _view.SetMission(GetTitle(_subscribedMission), progressText, !isTimerTick);
    }

    private static bool IsContinuouslyUpdating(MissionBase mission)
    {
        return mission is SurvivalMission or AltarDefenseMission or CaptureZoneMission
            && !mission.IsCompleted && !mission.IsFailed;
    }

    private static string GetTitle(MissionBase mission)
    {
        return mission switch
        {
            EnemyKillMission => "Defeat Enemies",
            SoulCollectMission => "Collect Souls",
            AltarDefenseMission => "Defend the Altar",
            SurvivalMission => "Survive",
            CaptureZoneMission => "Capture the Zone",
            _ => "Mission"
        };
    }
}
