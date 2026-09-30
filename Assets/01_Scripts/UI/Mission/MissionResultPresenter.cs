using UnityEngine;

/// <summary>
/// 미션 성공/실패 시 결과 패널을 보여준다.
/// 성공: 성공 패널 연출 (페이드 인 → 3초 → 페이드 아웃)
/// 실패: 실패 패널 연출 (천천히 페이드 인 → 3초) 후 EndingUI 표시
/// 스테이지 제한 시간이 끝나도(StageStatusSource.TimeExpired) 실패 연출을 보여준다.
/// (현재는 연출만 연결. 실제 미션 실패 처리는 팀원 구현 후 연결 예정)
///
/// 층마다 새 미션이 시작되므로(MissionManager.OnMissionInitialized) 그때마다 새 미션의 성공/실패 이벤트로 구독을 옮긴다.
/// 새 미션이 시작되면 성공 연출을 다시 보여줄 수 있다. (실패는 결과창으로 넘어가므로 한 번만)
/// </summary>
[DisallowMultipleComponent]
public class MissionResultPresenter : MonoBehaviour
{
    [SerializeField] private FadePanelView _successPanel;
    [SerializeField] private FadePanelView _failedPanel;

    [Tooltip("실패 연출이 끝나면 켤 결과 화면. 꺼진 상태로 씬에 배치한다.")]
    [SerializeField] private GameObject _endingUI;

    [Tooltip("제한 시간이 끝나면(TimeExpired) 실패 연출을 보여준다. 비워두면 시간 초과를 처리하지 않는다.")]
    [SerializeField] private StageStatusSource _stageSource;

    private MissionManager _missionManager;
    private MissionBase _subscribedMission;
    private bool _hasShownResult;
    private bool _hasShownFailure;

    private void Awake()
    {
        // 씬에 켜진 상태로 저장되어 있어도 결과 화면은 꺼진 상태로 시작한다.
        if (_endingUI != null)
        {
            _endingUI.SetActive(false);
        }
    }

    private void Start()
    {
        if (_successPanel == null || _failedPanel == null)
        {
            Debug.LogError("[MissionResultPresenter] 성공/실패 패널이 할당되지 않았습니다.", this);
        }

        if (_endingUI == null)
        {
            Debug.LogWarning("[MissionResultPresenter] EndingUI가 할당되지 않아 실패 후 결과 화면을 띄우지 않습니다.", this);
        }

        if (_failedPanel != null)
        {
            _failedPanel.Finished += HandleFailedPanelFinished;
        }

        if (_stageSource != null)
        {
            _stageSource.TimeExpired += ShowFailure;
        }

        _missionManager = MissionManager.Instance;
        if (_missionManager == null)
        {
            return;
        }

        _missionManager.OnMissionInitialized += HandleMissionInitialized;

        // MissionManager.Start가 먼저 실행되어 이미 미션이 있으면 바로 연결한다.
        SubscribeMission(_missionManager.GetCurrentMission());
    }

    private void OnDestroy()
    {
        if (_failedPanel != null)
        {
            _failedPanel.Finished -= HandleFailedPanelFinished;
        }

        if (_stageSource != null)
        {
            _stageSource.TimeExpired -= ShowFailure;
        }

        if (_missionManager != null)
        {
            _missionManager.OnMissionInitialized -= HandleMissionInitialized;
        }

        SubscribeMission(null);
    }

    public void ShowSuccess()
    {
        if (_hasShownResult || _successPanel == null)
        {
            return;
        }

        _hasShownResult = true;
        _successPanel.Play();
    }

    public void ShowFailure()
    {
        if (_hasShownResult || _failedPanel == null)
        {
            return;
        }

        _hasShownResult = true;
        _hasShownFailure = true;
        _failedPanel.Play();
    }

    /// <summary>
    /// [테스트용] 결과 표시 상태를 초기화해 다시 연출을 볼 수 있게 한다.
    /// </summary>
    public void ResetForTest()
    {
        _hasShownResult = false;
        _hasShownFailure = false;

        if (_successPanel != null)
        {
            _successPanel.Hide();
        }

        if (_failedPanel != null)
        {
            _failedPanel.Hide();
        }

        if (_endingUI != null)
        {
            _endingUI.SetActive(false);
        }
    }

    private void HandleMissionInitialized(MissionBase mission)
    {
        SubscribeMission(mission);

        // 새 미션이 시작되면 성공 연출을 다시 보여줄 수 있게 한다. 실패 후에는 결과창으로 넘어가므로 그대로 둔다.
        if (!_hasShownFailure)
        {
            _hasShownResult = false;
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
            _subscribedMission.OnMissionCompleted -= ShowSuccess;
            _subscribedMission.OnMissionFailed -= ShowFailure;
        }

        _subscribedMission = mission;

        if (_subscribedMission != null)
        {
            _subscribedMission.OnMissionCompleted += ShowSuccess;
            _subscribedMission.OnMissionFailed += ShowFailure;
        }
    }

    private void HandleFailedPanelFinished()
    {
        if (_endingUI != null)
        {
            _endingUI.SetActive(true);
        }
    }
}
