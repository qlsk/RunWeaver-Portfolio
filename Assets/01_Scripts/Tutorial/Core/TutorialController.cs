using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    // 건너뛰면 장착한 원소 룬 효과가 두 개가 되도록, 없는 만큼 이 순서로 채운다.
    private static readonly string[] s_skipDefaultElementIds = { "rune_fire", "rune_wind" };
    private const int SkipElementCount = 2;

    [Header("참조")]
    [SerializeField] private TutorialStepSO[] _steps;
    [SerializeField] private TutorialGuideCardView _guideCardView;
    [SerializeField] private TutorialKeyHintView _keyHintView;
    [SerializeField] private TutorialSpotlightView _spotlightView;
    [SerializeField] private TutorialWorldMarkerView _worldMarkerView;
    [SerializeField] private TutorialRecordTracker _recordTracker;

    [Header("월드 표식 (다른 안내와 같이 띄울 때)")]
    [Tooltip("단계의 안내가 월드 표식이 아니어도 표식 대상(Marker Target)이 있으면 그 위에 표식을 같이 띄운다. 이때 쓰는 글자")]
    [SerializeField] private string _statueMarkerText = "시작 석상";
    [SerializeField] private string _portalMarkerText = "다음 층";

    [Header("룬석 지급")]
    [SerializeField] private string[] _grantedRuneWordIds;

    [Header("주력 원소 제한")]
    [Tooltip("튜토리얼 중 주력 원소 선택에서 막을 원소")]
    [SerializeField] private ReactionElement[] _lockedPrimaryElements;

    [Header("임무 실패 없음")]
    [SerializeField][Range(0f, 1f)] private float _minHealthRatio = 0.2f;
    [SerializeField][Min(1f)] private float _timeoutExtendSeconds = 30f;

    private Coroutine _startRoutine;
    private StageManager _stageManager;
    private ITutorialCondition _activeCondition;
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
            Debug.LogError("[TutorialController] 단계 목록이 비어 있습니다.", this);
            enabled = false;
            return;
        }

        if (_steps.Any(step => step == null))
        {
            Debug.LogError("[TutorialController] 단계 목록에 비어 있는 항목이 있습니다.", this);
            enabled = false;
            return;
        }

        _startRoutine = StartCoroutine(StartRoutine());
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        _activeCondition?.Tick();
    }

    private void OnDisable()
    {
        if (_startRoutine != null)
            StopCoroutine(_startRoutine);

        _startRoutine = null;
        EndActiveCondition();
        ReleaseRules();

        if (_stageManager != null)
            _stageManager.MissionCleared -= HandleMissionCleared;

        if (!IsRunning)
            return;

        IsRunning = false;

        if (_guideCardView != null)
            _guideCardView.Hide();
    }

    public void Skip()
    {
        if (!IsRunning)
            return;

        RuneEffectLoadout loadout = PlayerRuneManager.Instance.RuneEffectLoadout;
        var slotIds = new List<string>(loadout.EquippedEffectIds);
        foreach (string defaultId in s_skipDefaultElementIds)
        {
            if (slotIds.Count >= SkipElementCount)
                break;

            if (!slotIds.Contains(defaultId))
                slotIds.Add(defaultId);
        }

        loadout.ConfirmEquipment(slotIds);

        if (_stageManager.IsWaitingForPreparation)
        {
            var statue = FindAnyObjectByType<ExpeditionStatue>();
            statue.SkipPreparation();
        }

        TutorialProgressStore.MarkCompleted();
        enabled = false;
    }

    private IEnumerator StartRoutine()
    {
        yield return null;

        while (PortalPlayerHider.IsAnyHiding)
            yield return null;

        _stageManager = StageManager.Instance;
        if (!_stageManager.IsWaitingForPreparation)
        {
            enabled = false;
            yield break;
        }

        _startRoutine = null;
        IsRunning = true;

        _stageManager.MissionCleared += HandleMissionCleared;

        _rules = new TutorialRules(_grantedRuneWordIds, _minHealthRatio, _timeoutExtendSeconds, _recordTracker,
            _lockedPrimaryElements);
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
                _keyHintView.Show(step);
                break;
            case TutorialOverlayKind.WorldMarker:
                _worldMarkerView.Show(step);
                break;
            case TutorialOverlayKind.Spotlight:
                _spotlightView.Show(step);
                break;
        }

        // 안내가 월드 표식이 아니어도 표식 대상이 정해져 있으면 대상 위에 표식을 같이 띄운다. (1단계 : 이동 안내 + 시작 석상)
        if (step.Overlay != TutorialOverlayKind.WorldMarker && step.MarkerTarget != TutorialMarkerTarget.None && _worldMarkerView != null)
        {
            _worldMarkerView.Show(FindWorldMarkerPrefab(), step.MarkerTarget, MarkerText(step.MarkerTarget));
        }
    }

    /// <summary>월드 표식 프리팹. 월드 표식을 쓰는 단계(다음 층)의 프리팹을 같이 쓴다.</summary>
    private GameObject FindWorldMarkerPrefab()
    {
        TutorialStepSO markerStep = Array.Find(_steps, step => step != null && step.Overlay == TutorialOverlayKind.WorldMarker && step.OverlayPrefab != null);
        return markerStep != null ? markerStep.OverlayPrefab : null;
    }

    private string MarkerText(TutorialMarkerTarget target)
    {
        return target switch
        {
            TutorialMarkerTarget.StartStatue => _statueMarkerText,
            TutorialMarkerTarget.Portal => _portalMarkerText,
            _ => string.Empty
        };
    }

    private void HideOverlay()
    {
        if (_keyHintView != null)
            _keyHintView.Hide();

        if (_spotlightView != null)
            _spotlightView.Hide();

        if (_worldMarkerView != null)
            _worldMarkerView.Hide();
    }

    private void HandleMissionCleared(int depth)
    {
        int rewardStepIndex = Array.FindIndex(_steps, step => step.Condition == TutorialConditionKind.RewardConfirm);
        if (CurrentIndex >= rewardStepIndex)
            return;
        enabled = false;
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

    private void ReleaseRules()
    {
        if (_rules == null)
            return;

        _rules.Release();
        _rules = null;
    }

    [ContextMenu("Reset Progress")]
    private void ResetProgress()
    {
        TutorialProgressStore.ResetForDebug();
    }
}