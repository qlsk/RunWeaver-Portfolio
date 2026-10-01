using UnityEngine;

/// <summary>
/// MissionManager의 현재 미션 진행 상황을 HUD 미션 두루마리에 표시한다.
/// 층마다 새 미션이 시작되므로(OnMissionInitialized) 그때마다 새 미션의 진행 이벤트로 구독을 옮긴다.
/// 미션의 진행 문구는 한글이라 TMP 폰트로 표시할 수 없으므로 MissionHudText로 영문 문구를 만든다.
/// 씬에 MissionManager가 없으면 아무것도 하지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class MissionHudBinder : MonoBehaviour
{
    [SerializeField] private MissionScrollView _view;

    private MissionManager _missionManager;
    private MissionBase _subscribedMission;

    private void Start()
    {
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
            _view.SetMission(GetTitle(mission), MissionHudText.GetProgressText(mission));
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
        _view.SetMission(GetTitle(_subscribedMission), MissionHudText.GetProgressText(_subscribedMission));
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
