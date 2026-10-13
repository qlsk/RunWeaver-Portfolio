using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TutorialStepSO[] _steps;
    [SerializeField] private TutorialGuideCardView _guideCardView;
    [SerializeField] private TutorialKeyHintView _keyHintView;
    [SerializeField] private TutorialSpotlightView _spotlightView;
    [SerializeField] private TutorialWorldMarkerView _worldMarkerView;
    [SerializeField] private TutorialRecordTracker _recordTracker;

    [Header("룬석 지급")]
    [SerializeField] private string[] _grantedRuneWordIds;

    [Header("건너뛰기")]
    [SerializeField] private string _skipDefaultElementId = "rune_fire";
    [SerializeField] private string[] _skipDefaultRuneWordIds = { "rune_water" };

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

        EquipSkipDefaults();

        if (_stageManager != null && _stageManager.IsWaitingForPreparation)
        {
            ExpeditionStatue statue = FindAnyObjectByType<ExpeditionStatue>();
            statue.SkipPreparation();
        }

        TutorialProgressStore.MarkCompleted();
        enabled = false;
    }

    private void EquipSkipDefaults()
    {
        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        RuneEffectLoadout loadout = runeManager.RuneEffectLoadout;
        loadout.EquipPrimaryElement(_skipDefaultElementId);
        EquipSkipDefaultRuneWords(loadout);
    }

    private void EquipSkipDefaultRuneWords(RuneEffectLoadout loadout)
    {
        if (_skipDefaultRuneWordIds == null)
            return;

        string[] slotIds = new string[loadout.ActiveSlotCount];
        for (int i = 0; i < slotIds.Length && i < loadout.EquippedEffectIds.Count; i++)
            slotIds[i] = loadout.EquippedEffectIds[i];

        bool isChanged = false;
        foreach (string runeWordId in _skipDefaultRuneWordIds)
        {
            if (string.IsNullOrEmpty(runeWordId) || Array.IndexOf(slotIds, runeWordId) >= 0)
                continue;

            int slot = loadout.FindEquipSlot(slotIds, runeWordId, out EquipError error);
            if (slot < 0)
            {
                Debug.LogWarning($"[TutorialController] 기본 룬 워드 '{runeWordId}'를 장착할 수 없습니다: {error}", this);
                continue;
            }

            for (int i = slotIds.Length - 1; i > slot; i--)
                slotIds[i] = slotIds[i - 1];

            slotIds[slot] = runeWordId;
            isChanged = true;
        }

        if (isChanged)
            loadout.ConfirmEquipment(slotIds);
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

        _rules = new TutorialRules(_grantedRuneWordIds, _minHealthRatio, _timeoutExtendSeconds, _recordTracker);
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

    // 석상 조작(상호작용, 원소 확정, 룬 확정)이 단계보다 먼저 일어나는 경우는 감시하지 않는다.
    // 선택 창과 룬북이 석상 상호작용 뒤에만 열리고, 시작 위치(0, -4)에서 석상 상호작용 범위까지의 여유(약 0.39)가
    // 1단계 이동량(0.1)보다 크기 때문이다. 시작 위치나 1단계 이동량을 바꾸면 이 전제를 다시 확인해야 한다.
    // 임무는 UI가 막아 주지 않으므로, 보상 확정 단계보다 먼저 클리어하면 이후 단계가 멈추지 않도록 중단한다.
    private void HandleMissionCleared(int depth)
    {
        int rewardStepIndex = Array.FindIndex(_steps, step => step.Condition == TutorialConditionKind.RewardConfirm);
        if (CurrentIndex >= rewardStepIndex)
            return;

        Debug.LogWarning("[TutorialController] 보상 확정 단계보다 먼저 임무를 클리어해 튜토리얼을 중단합니다.");
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