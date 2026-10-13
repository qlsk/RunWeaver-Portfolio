using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private TutorialStepSO[] _steps;
    [SerializeField] private TutorialGuideCardView _guideCardView;
    [SerializeField] private TutorialKeyHintView _keyHintView;
    [SerializeField] private TutorialSpotlightView _spotlightView;
    [SerializeField] private TutorialWorldMarkerView _worldMarkerView;

    [Header("룬석 지급")]
    [Tooltip("룬 장착 단계가 시작될 때 룬 문자를 지급할 룬 효과 id")]
    [SerializeField] private string[] _grantedRuneWordIds;

    [Header("임무 실패 없음")]
    [Tooltip("피해로는 이 비율(최대 체력 대비) 아래로 내려가지 않는다. 값은 아직 정해지지 않은 임시값")]
    [SerializeField, Range(0f, 1f)] private float _minHealthRatio = 0.2f;
    [Tooltip("제한 시간이 끝났을 때 늘려 주는 시간(초). 값은 아직 정해지지 않은 임시값")]
    [SerializeField, Min(1f)] private float _timeoutExtendSeconds = 30f;

    private Coroutine _startRoutine;
    private StageManager _stageManager;
    private ITutorialCondition _activeCondition;
    private TutorialPreparationWatcher _preparationWatcher;
    private TutorialRules _rules;

    public int CurrentIndex { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action<int, TutorialStepSO> StepStarted;
    public event Action Finished;

    private void Start()
    {
        if (TutorialProgressStore.IsCompleted)
        {
            enabled = false;
            return;
        }

        if (_steps == null || _steps.Length == 0)
        {
            Debug.LogError("Tutorial Steps have not been set.");
            enabled = false;
            return;
        }

        if (_steps.Any(step => step == null))
        {
            Debug.LogError("Steps have not been set.");
            enabled = false;
            return;
        }

        _startRoutine = StartCoroutine(StartRoutine());
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        if (_activeCondition == null)
            return;

        if (IsStageStartedEarly())
        {
            Debug.LogWarning("[TutorialController] 출발 준비 단계 중 1층이 시작되어 튜토리얼을 중단합니다.");
            enabled = false;
            return;
        }

        _activeCondition.Tick();
    }

    private void OnDisable()
    {
        if (_startRoutine != null)
            StopCoroutine(_startRoutine);

        _startRoutine = null;
        EndActiveCondition();
        EndPreparationWatcher();
        ReleaseRules();

        if (!IsRunning)
            return;

        // Finish를 거치지 않은 중단이다. 완료 기록은 남기지 않는다.
        IsRunning = false;

        // 씬 파괴 중에는 카드 뷰가 먼저 파괴됐을 수 있다.
        if (_guideCardView != null)
            _guideCardView.Hide();
    }

    private IEnumerator StartRoutine()
    {
        yield return null;

        // StageManager.Instance는 없으면 새로 만들거나 예외를 던지므로, 생성 없이 기존 인스턴스만 찾는다.
        _stageManager = FindAnyObjectByType<StageManager>();
        if (_stageManager == null)
        {
            Debug.LogWarning("[TutorialController] StageManager 객체가 없습니다.");
            enabled = false;
            yield break;
        }

        if (!_stageManager.IsWaitingForPreparation)
        {
            Debug.LogWarning("[TutorialController] 이미 1층이 시작됐습니다.");
            enabled = false;
            yield break;
        }

        while (PortalPlayerHider.IsAnyHiding)
            yield return null;

        if (_stageManager == null || !_stageManager.IsWaitingForPreparation)
        {
            Debug.LogWarning("[TutorialController] 이미 1층이 시작됐습니다.");
            enabled = false;
            yield break;
        }

        _startRoutine = null;
        IsRunning = true;

        _preparationWatcher = new TutorialPreparationWatcher();
        _preparationWatcher.OutOfOrderDetected += HandleOutOfOrderDetected;
        _preparationWatcher.Begin(() => _steps[CurrentIndex].Condition);

        _rules = new TutorialRules(_grantedRuneWordIds, _minHealthRatio, _timeoutExtendSeconds);
        _rules.Apply();

        BeginStep(0);
    }

    private void BeginStep(int index)
    {
        CurrentIndex = index;
        TutorialStepSO step = _steps[index];

        ITutorialCondition condition = TutorialConditionFactory.Create(step.Condition);
        if (condition == null)
        {
            enabled = false;
            return;
        }

        _activeCondition = condition;
        _rules.OnStepStarted(step);
        StepStarted?.Invoke(index, step);

        _guideCardView.Show(step, index + 1, _steps.Length);
        ShowOverlay(step);

        condition.Completed += HandleConditionCompleted;
        condition.Begin(step);
    }

    private void HandleConditionCompleted()
    {
        CompleteStep();
    }

    private void CompleteStep()
    {
        EndActiveCondition();

        if (CurrentIndex + 1 < _steps.Length)
            BeginStep(CurrentIndex + 1);
        else
            Finish();
    }

    private void Finish()
    {
        _guideCardView.Hide();
        TutorialProgressStore.MarkCompleted();
        IsRunning = false;
        Finished?.Invoke();
        enabled = false;
    }

    private void ShowOverlay(TutorialStepSO step)
    {
        switch (step.Overlay)
        {
            case TutorialOverlayKind.KeyHint:
                if (_keyHintView == null)
                    Debug.LogError("[TutorialController] 키 힌트 뷰가 연결되지 않았습니다.", this);
                else
                    _keyHintView.Show(step);
                break;
            case TutorialOverlayKind.WorldMarker:
                if (_worldMarkerView == null)
                    Debug.LogError("[TutorialController] 월드 표식 뷰가 연결되지 않았습니다.", this);
                else
                    _worldMarkerView.Show(step);
                break;
            case TutorialOverlayKind.Spotlight:
                if (_spotlightView == null)
                    Debug.LogError("[TutorialController] 스포트라이트 뷰가 연결되지 않았습니다.", this);
                else
                    _spotlightView.Show(step);
                break;
        }
    }

    private void HideOverlay()
    {
        // 씬 파괴 중에는 뷰가 먼저 파괴됐을 수 있다.
        if (_keyHintView != null)
            _keyHintView.Hide();

        if (_spotlightView != null)
            _spotlightView.Hide();

        if (_worldMarkerView != null)
            _worldMarkerView.Hide();
    }

    private void HandleOutOfOrderDetected()
    {
        Debug.LogWarning("[TutorialController] 현재 단계보다 앞선 석상 조작이 감지되어 튜토리얼을 중단합니다.");
        enabled = false;
    }

    private void ReleaseRules()
    {
        if (_rules == null)
            return;

        _rules.Release();
        _rules = null;
    }

    private void EndPreparationWatcher()
    {
        if (_preparationWatcher == null)
            return;

        _preparationWatcher.OutOfOrderDetected -= HandleOutOfOrderDetected;
        _preparationWatcher.End();
        _preparationWatcher = null;
    }

    private bool IsStageStartedEarly()
    {
        if (!_steps[CurrentIndex].Condition.IsPreparationPhase())
            return false;

        return _stageManager == null || !_stageManager.IsWaitingForPreparation;
    }

    private void EndActiveCondition()
    {
        if (_activeCondition == null)
            return;

        _activeCondition.Completed -= HandleConditionCompleted;
        _activeCondition.End();
        _activeCondition = null;
        HideOverlay();
    }

    [ContextMenu("Reset Progress")]
    private void ResetProgress()
    {
        TutorialProgressStore.ResetForDebug();
    }
}