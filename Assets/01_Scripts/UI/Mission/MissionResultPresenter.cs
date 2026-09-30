using UnityEngine;

/// <summary>
/// 미션 성공/실패 시 결과 패널을 보여준다.
/// 성공: 성공 패널 연출 (페이드 인 → 3초 → 페이드 아웃)
/// 실패: 실패 패널 연출 (천천히 페이드 인 → 3초) 후 EndingUI 표시
///
/// MissionManager의 현재 미션은 MissionManager.Start에서 만들어지므로,
/// 첫 진행 이벤트(OnMissionProgressChanged)가 왔을 때 미션의 성공/실패 이벤트를 구독한다.
/// </summary>
[DisallowMultipleComponent]
public class MissionResultPresenter : MonoBehaviour
{
    [SerializeField] private FadePanelView _successPanel;
    [SerializeField] private FadePanelView _failedPanel;

    [Tooltip("실패 연출이 끝나면 켤 결과 화면. 꺼진 상태로 씬에 배치한다.")]
    [SerializeField] private GameObject _endingUI;

    private MissionManager _missionManager;
    private MissionBase _subscribedMission;
    private bool _hasShownResult;

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

        _missionManager = MissionManager.Instance;
        if (_missionManager == null)
        {
            return;
        }

        _missionManager.OnMissionProgressChanged += HandleMissionProgressChanged;
        SubscribeMission(_missionManager.GetCurrentMission());
    }

    private void OnDestroy()
    {
        if (_failedPanel != null)
        {
            _failedPanel.Finished -= HandleFailedPanelFinished;
        }

        if (_missionManager != null)
        {
            _missionManager.OnMissionProgressChanged -= HandleMissionProgressChanged;
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
        _failedPanel.Play();
    }

    /// <summary>
    /// [테스트용] 결과 표시 상태를 초기화해 다시 연출을 볼 수 있게 한다.
    /// </summary>
    public void ResetForTest()
    {
        _hasShownResult = false;

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

    private void HandleMissionProgressChanged(float progressRatio, string progressText)
    {
        SubscribeMission(_missionManager.GetCurrentMission());
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
