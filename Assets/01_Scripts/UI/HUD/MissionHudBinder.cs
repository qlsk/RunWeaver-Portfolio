using UnityEngine;

/// <summary>
/// MissionManager의 미션 진행 상황을 HUD 미션 두루마리에 표시한다.
/// 씬에 MissionManager가 없으면 아무것도 하지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class MissionHudBinder : MonoBehaviour
{
    [SerializeField] private MissionScrollView _view;

    private MissionManager _missionManager;

    private void Start()
    {
        // MissionManager.Instance는 MissionManager.Awake에서 설정되므로 Start에서 찾는다.
        _missionManager = MissionManager.Instance;
        if (_missionManager == null || _view == null)
        {
            return;
        }

        _missionManager.OnMissionProgressChanged += HandleMissionProgressChanged;

        MissionBase currentMission = _missionManager.GetCurrentMission();
        if (currentMission != null)
        {
            _view.SetMission(GetTitle(currentMission), currentMission.GetProgressText());
        }
    }

    private void OnDestroy()
    {
        if (_missionManager != null)
        {
            _missionManager.OnMissionProgressChanged -= HandleMissionProgressChanged;
        }
    }

    private void HandleMissionProgressChanged(float progressRatio, string progressText)
    {
        _view.SetMission(GetTitle(_missionManager.GetCurrentMission()), progressText);
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
