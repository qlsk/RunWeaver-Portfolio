using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>튜토리얼 필수 단계를 순서대로 진행한다. 시작 대기, 단계 전환, 오버레이, 규칙, 건너뛰기를 맡고 완료·중단 정리는 OnDisable에서 한다.</summary>
public class TutorialController : MonoBehaviour
{
    private const string AlreadyStartedMessage = "이미 1층이 시작됐습니다.";

    [SerializeField] private TutorialStepSO[] _steps;
    [SerializeField] private TutorialGuideCardView _guideCardView;
    [SerializeField] private TutorialKeyHintView _keyHintView;
    [SerializeField] private TutorialSpotlightView _spotlightView;
    [SerializeField] private TutorialWorldMarkerView _worldMarkerView;
    [Tooltip("상황 안내 카드 추적기. 시간 연장 안내에 쓰며 비워 두면 그 카드만 건너뛴다")]
    [SerializeField] private TutorialRecordTracker _recordTracker;

    [Header("룬석 지급")]
    [Tooltip("룬 장착 단계가 시작될 때 룬 문자를 지급할 룬 효과 id")]
    [SerializeField] private string[] _grantedRuneWordIds;

    [Header("건너뛰기")]
    [Tooltip("건너뛰면 주 원소가 비어 있을 때 장착하는 기본 원소 룬 효과 id")]
    [SerializeField] private string _skipDefaultElementId = "rune_fire";
    [Tooltip("건너뛰면 자리가 있을 때 추가로 장착하는 기본 룬 워드 id. 값은 아직 정해지지 않은 임시값")]
    [SerializeField] private string[] _skipDefaultRuneWordIds = { "rune_water" };

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

        if (_activeCondition == null)
            return;

        if (IsStageStartedEarly())
        {
            StopWithWarning("출발 준비 단계 중 1층이 시작되어 튜토리얼을 중단합니다.");
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
            StopWithWarning("StageManager 객체가 없습니다.");
            yield break;
        }

        if (!_stageManager.IsWaitingForPreparation)
        {
            StopWithWarning(AlreadyStartedMessage);
            yield break;
        }

        while (PortalPlayerHider.IsAnyHiding)
            yield return null;

        if (_stageManager == null || !_stageManager.IsWaitingForPreparation)
        {
            StopWithWarning(AlreadyStartedMessage);
            yield break;
        }

        _startRoutine = null;
        IsRunning = true;

        _preparationWatcher = new TutorialPreparationWatcher();
        _preparationWatcher.OutOfOrderDetected += HandleOutOfOrderDetected;
        _preparationWatcher.Begin(() => _steps[CurrentIndex].Condition);

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

    /// <summary>
    /// 튜토리얼을 건너뛴다. 기본 원소와 룬 워드를 장착하고, 출발 준비 중이면 석상을 건너뛰어 1층을 시작한 뒤 완료로 기록한다.
    /// 컨트롤러를 끄므로 규칙 해제와 화면 정리는 OnDisable이 맡는다.
    /// </summary>
    public void Skip()
    {
        if (!IsRunning)
            return;

        EquipSkipDefaults();

        if (_stageManager != null && _stageManager.IsWaitingForPreparation)
        {
            ExpeditionStatue statue = FindAnyObjectByType<ExpeditionStatue>();
            if (statue != null)
                statue.SkipPreparation();
            else
                Debug.LogWarning("[TutorialController] ExpeditionStatue를 찾을 수 없어 출발 준비를 건너뛰지 못합니다.", this);
        }

        TutorialProgressStore.MarkCompleted();
        enabled = false;
    }

    private void EquipSkipDefaults()
    {
        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        if (runeManager == null || !runeManager.IsInitialized)
        {
            Debug.LogWarning("[TutorialController] 룬 시스템이 준비되지 않아 기본 장착을 하지 못합니다.", this);
            return;
        }

        // 규칙에 어긋나는 기본값이 건너뛰기 전체를 막지 않도록, 장착 실패는 경고만 남기고 넘어간다.
        try
        {
            RuneEffectLoadout loadout = runeManager.RuneEffectLoadout;
            if (string.IsNullOrEmpty(loadout.GetEquippedEffectId(RuneEffectLoadout.PrimaryElementSlotIndex)))
                loadout.EquipPrimaryElement(_skipDefaultElementId);

            EquipSkipDefaultRuneWords(loadout);
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogWarning($"[TutorialController] 기본 장착을 하지 못했습니다: {exception.Message}", this);
        }
    }

    /// <summary>기본 룬 워드를 룬북과 같은 장착 규칙(원소 수, 삽입 위치, 충돌)으로 검증해 빈 칸에 넣는다.</summary>
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

    /// <summary>경고를 남기고 컨트롤러를 끈다. 정리는 OnDisable이 맡는다.</summary>
    private void StopWithWarning(string message)
    {
        Debug.LogWarning($"[TutorialController] {message}", this);
        enabled = false;
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