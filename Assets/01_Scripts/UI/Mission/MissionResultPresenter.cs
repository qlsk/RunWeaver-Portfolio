using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 미션 성공 / 런 종료 시 결과 패널을 보여준다.
/// - 미션 성공 : 성공 패널 연출 (페이드 인 → 3초 → 페이드 아웃). 층마다 새 미션이 시작되면 다시 보여줄 수 있다.
///   부제(_successSubText)에 미션 클리어 소요 시간을 보여준다. (시작 오브젝트를 누른 순간부터, 일시정지 시간 제외)
/// - 보스 처치(런 클리어) : 성공 패널을 "탐사 완료" 문구로 보여준 뒤 결과 화면(EndingUI)을 켠다. (부제는 숨긴다)
/// - 실패(사망, 미션 실패, 탐사 포기) : _failureSequence가 있으면 종료 사유별 실패 연출(RunFailureSequence) 후 결과 화면을 켠다.
///   없으면 실패 패널 연출 (천천히 페이드 인 → 3초) 후 결과 화면을 켠다.
///   실패 패널의 글자는 종료 사유에 따라 바뀐다. (You Die / Mission Failed / Run Abandoned)
///   실패 패널은 _failedPanelMaxAlpha(80%)까지만 진해진다.
/// - 미션 성공 연출 중이거나 정화 대기 중에 죽어도 실패 연출을 보여준다. (성공 패널을 숨긴다)
///
/// 런 종료는 StageManager.RunEnded로 받는다. PlayerDeathResultTrigger가 사망 시 ShowFailure를 불러도 된다.
/// </summary>
[DisallowMultipleComponent]
public class MissionResultPresenter : MonoBehaviour
{
    [SerializeField] private FadePanelView _successPanel;
    [SerializeField] private FadePanelView _failedPanel;

    [Tooltip("할당하면 실패 패널 대신 종료 사유별 실패 연출을 보여준다. (피격 사망 / 탐사 포기 / 임무 실패)")]
    [SerializeField] private RunFailureSequence _failureSequence;

    [Tooltip("실패 연출이 끝나면 켤 결과 화면. 꺼진 상태로 씬에 배치한다.")]
    [SerializeField] private GameObject _endingUI;

    [Tooltip("사용하지 않는다. (제한 시간 초과는 StageManager가 미션 실패로 처리한다) 기존 씬 참조 호환용")]
    [SerializeField] private StageStatusSource _stageSource;

    [Header("패널 글자 (비워두면 패널 안의 TMP 텍스트를 찾는다)")]
    [SerializeField] private TMP_Text _successText;
    [SerializeField] private TMP_Text _failedText;
    [Tooltip("성공 패널의 부제. 미션 클리어 소요 시간을 보여준다. 비워두면 표시하지 않는다.")]
    [SerializeField] private TMP_Text _successSubText;

    [Header("문구")]
    [SerializeField] private string _missionCompleteText = "임무 완료";
    [SerializeField] private string _runClearText = "탐사 완료";
    [SerializeField] private string _youDieText = "사망";
    [SerializeField] private string _missionFailedText = "임무 실패";
    [SerializeField] private string _runAbandonedText = "탐사 포기";
    [Tooltip("클리어 소요 시간이 1분 미만일 때. {0} 초")]
    [SerializeField] private string _clearTimeSecondsFormat = "{0}초";
    [Tooltip("클리어 소요 시간이 1분 이상일 때. {0} 분, {1} 초")]
    [SerializeField] private string _clearTimeMinutesFormat = "{0}분 {1}초";

    [Header("실패 패널")]
    [Tooltip("실패 패널이 페이드 인으로 진해지는 최대 알파")]
    [SerializeField, Range(0f, 1f)] private float _failedPanelMaxAlpha = 0.8f;

    private MissionManager _missionManager;
    private StageManager _stageManager;
    private MissionBase _subscribedMission;
    private bool _hasShownSuccess;
    private bool _hasShownFailure;
    private bool _isShowingClear;
    private float _missionStartTime = -1f;

    /// <summary>성공 패널 연출(페이드 아웃 포함)이 끝났을 때</summary>
    public event Action SuccessFinished;

    /// <summary>미션 성공 연출이 진행 중인지 (포탈은 이 연출이 끝난 뒤에 나온다)</summary>
    public bool IsShowingSuccess => !_isShowingClear && _successPanel != null && _successPanel.IsPlaying;

    private void Awake()
    {
        // 씬에 켜진 상태로 저장되어 있어도 결과 화면은 꺼진 상태로 시작한다.
        if (_endingUI != null)
        {
            _endingUI.SetActive(false);
        }

        if (_successText == null && _successPanel != null)
        {
            _successText = _successPanel.GetComponentInChildren<TMP_Text>(true);
        }

        if (_failedText == null && _failedPanel != null)
        {
            _failedText = _failedPanel.GetComponentInChildren<TMP_Text>(true);
        }

        if (_failedPanel != null)
        {
            _failedPanel.SetMaxAlpha(_failedPanelMaxAlpha);
        }
    }

    private void Start()
    {
        if (_successPanel == null || (_failedPanel == null && _failureSequence == null))
        {
            Debug.LogError("[MissionResultPresenter] 성공/실패 패널이 할당되지 않았습니다.", this);
        }

        if (_endingUI == null)
        {
            Debug.LogWarning("[MissionResultPresenter] EndingUI가 할당되지 않아 결과 화면을 띄우지 않습니다.", this);
        }

        if (_failedPanel != null)
        {
            _failedPanel.Finished += HandleFailedPanelFinished;
        }

        if (_successPanel != null)
        {
            _successPanel.Finished += HandleSuccessPanelFinished;
        }

        _stageManager = FindAnyObjectByType<StageManager>();
        if (_stageManager != null)
        {
            _stageManager.RunEnded += HandleRunEnded;
            _stageManager.PhaseChanged += HandlePhaseChanged;
        }

        _missionManager = FindAnyObjectByType<MissionManager>();
        if (_missionManager == null)
        {
            return;
        }

        _missionManager.OnMissionInitialized += HandleMissionInitialized;

        // MissionManager가 먼저 미션을 정했으면 바로 연결한다.
        SubscribeMission(_missionManager.GetCurrentMission());
    }

    private void OnDestroy()
    {
        if (_failedPanel != null)
        {
            _failedPanel.Finished -= HandleFailedPanelFinished;
        }

        if (_successPanel != null)
        {
            _successPanel.Finished -= HandleSuccessPanelFinished;
        }

        if (_stageManager != null)
        {
            _stageManager.RunEnded -= HandleRunEnded;
            _stageManager.PhaseChanged -= HandlePhaseChanged;
        }

        if (_missionManager != null)
        {
            _missionManager.OnMissionInitialized -= HandleMissionInitialized;
        }

        SubscribeMission(null);
    }

    /// <summary>미션 성공 연출 ("임무 완료")</summary>
    public void ShowSuccess()
    {
        if (_hasShownSuccess || _hasShownFailure || _successPanel == null)
        {
            return;
        }

        _hasShownSuccess = true;
        SetText(_successText, _missionCompleteText);
        ShowSubText(GetClearTimeText());
        _successPanel.Play();
    }

    /// <summary>
    /// 실패 연출. 종료 사유는 RunFlow에 기록된 값을 쓰고, 없으면 사망으로 본다.
    /// </summary>
    public void ShowFailure()
    {
        ShowFailure(RunFlow.EndReason ?? RunEndReason.Died);
    }

    public void ShowFailure(RunEndReason reason)
    {
        if (_hasShownFailure || _isShowingClear || (_failedPanel == null && _failureSequence == null))
        {
            return;
        }

        _hasShownFailure = true;

        // 성공 연출 중에 죽어도 실패 연출을 보여준다.
        if (_successPanel != null && _successPanel.gameObject.activeSelf)
        {
            _successPanel.Hide();
        }

        if (_failureSequence != null)
        {
            _failureSequence.Play(reason, ShowEnding);
            return;
        }

        SetText(_failedText, GetFailureText(reason));
        _failedPanel.Play();
    }

    /// <summary>보스 처치 : "탐사 완료" 연출 후 결과 화면</summary>
    public void ShowClear()
    {
        if (_hasShownFailure || _isShowingClear)
        {
            return;
        }

        _isShowingClear = true;

        if (_successPanel == null)
        {
            ShowEnding();
            return;
        }

        SetText(_successText, _runClearText);
        ShowSubText(null);
        _successPanel.Play();
    }

    /// <summary>
    /// [테스트용] 결과 표시 상태를 초기화해 다시 연출을 볼 수 있게 한다.
    /// </summary>
    public void ResetForTest()
    {
        _hasShownSuccess = false;
        _hasShownFailure = false;
        _isShowingClear = false;

        if (_successPanel != null)
        {
            _successPanel.Hide();
        }

        if (_failedPanel != null)
        {
            _failedPanel.Hide();
        }

        if (_failureSequence != null)
        {
            _failureSequence.ResetForTest();
        }

        if (_endingUI != null)
        {
            _endingUI.SetActive(false);
        }
    }

    private void HandleRunEnded(RunEndReason reason)
    {
        if (reason == RunEndReason.Cleared)
        {
            ShowClear();
        }
        else
        {
            ShowFailure(reason);
        }
    }

    /// <summary>시작 오브젝트를 눌러 미션이 시작된 시각을 기록한다. (클리어 소요 시간 계산용)</summary>
    private void HandlePhaseChanged(StagePhase phase)
    {
        if (phase == StagePhase.InMission)
        {
            _missionStartTime = Time.time;
        }
    }

    /// <summary>미션 클리어 소요 시간 문구. 시작 시각을 모르면 null.</summary>
    private string GetClearTimeText()
    {
        if (_missionStartTime < 0f)
        {
            return null;
        }

        // Time.time은 일시정지(timeScale 0) 동안 흐르지 않는다.
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(Time.time - _missionStartTime));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return minutes > 0
            ? string.Format(_clearTimeMinutesFormat, minutes, seconds)
            : string.Format(_clearTimeSecondsFormat, seconds);
    }

    /// <summary>부제를 보여준다. 문구가 비어 있으면 숨긴다.</summary>
    private void ShowSubText(string value)
    {
        if (_successSubText == null)
        {
            return;
        }

        bool hasText = !string.IsNullOrEmpty(value);
        _successSubText.gameObject.SetActive(hasText);
        if (hasText)
        {
            _successSubText.text = value;
        }
    }

    private void HandleMissionInitialized(MissionBase mission)
    {
        SubscribeMission(mission);

        // 새 미션이 정해지면 성공 연출을 다시 보여줄 수 있게 한다.
        _hasShownSuccess = false;
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
        }

        _subscribedMission = mission;

        if (_subscribedMission != null)
        {
            _subscribedMission.OnMissionCompleted += ShowSuccess;
        }
    }

    private string GetFailureText(RunEndReason reason)
    {
        return reason switch
        {
            RunEndReason.MissionFailed => _missionFailedText,
            RunEndReason.Abandoned => _runAbandonedText,
            _ => _youDieText
        };
    }

    private void HandleSuccessPanelFinished()
    {
        if (_isShowingClear)
        {
            ShowEnding();
            return;
        }

        SafeEventNotifier.Notify(SuccessFinished);
    }

    private void HandleFailedPanelFinished()
    {
        ShowEnding();
    }

    private void ShowEnding()
    {
        if (_endingUI != null)
        {
            _endingUI.SetActive(true);
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
