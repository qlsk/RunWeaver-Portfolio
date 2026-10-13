using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private TutorialStepSO[] _steps;
    [SerializeField] private TutorialGuideCardView _guideCardView;

    private Coroutine _startRoutine;
    private ITutorialCondition _activeCondition;

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

        _activeCondition.Tick();
    }

    private void OnDisable()
    {
        if (_startRoutine != null)
            StopCoroutine(_startRoutine);

        _startRoutine = null;
        EndActiveCondition();
    }

    private IEnumerator StartRoutine()
    {
        yield return null;

        if (StageManager.Instance == null)
        {
            Debug.LogWarning("[TutorialController] StageManager 객체가 없습니다.");
            enabled = false;
            yield break;
        }

        if (!StageManager.Instance.IsWaitingForPreparation)
        {
            Debug.LogWarning("[TutorialController] 이미 1층이 시작됐습니다.");
            enabled = false;
            yield break;
        }

        while (PortalPlayerHider.IsAnyHiding)
            yield return null;

        if (!StageManager.Instance.IsWaitingForPreparation)
        {
            Debug.LogWarning("[TutorialController] 이미 1층이 시작됐습니다.");
            enabled = false;
            yield break;
        }

        _startRoutine = null;
        IsRunning = true;
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
        StepStarted?.Invoke(index, step);

        _guideCardView.Show(step, index + 1, _steps.Length);

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

    private void EndActiveCondition()
    {
        if (_activeCondition == null)
            return;

        _activeCondition.Completed -= HandleConditionCompleted;
        _activeCondition.End();
        _activeCondition = null;
    }

    [ContextMenu("Reset Progress")]
    private void ResetProgress()
    {
        TutorialProgressStore.ResetForDebug();
    }
}