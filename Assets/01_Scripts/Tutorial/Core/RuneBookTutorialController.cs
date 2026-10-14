using System.Collections;
using UnityEngine;

/// <summary>
/// 2층 준비 단계에서 룬북을 열고 추가 룬 효과를 장착하도록 안내한다. 한 번 마치면 다시 뜨지 않는다.
/// 1층 TutorialController와 별개로 동작하며 단계 SO, 조건 팩토리, 가이드 카드, 키 힌트를 재사용한다.
/// </summary>
public class RuneBookTutorialController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TutorialStepSO[] _steps;
    [SerializeField] private TutorialGuideCardView _guideCardView;
    [SerializeField] private TutorialKeyHintView _keyHintView;

    [Header("설정")]
    [SerializeField, Min(1)] private int _targetDepth = 2;
    [SerializeField] private string _grantedRuneWordId = "rune_chain";

    private StageManager _stageManager;
    private RuneBookV2Presenter _runeBookPresenter;
    private ITutorialCondition _activeCondition;
    private Coroutine _showRoutine;
    private int _currentStepIndex;
    private bool _isRunning;

    private void Start()
    {
        if (TutorialProgressStore.IsRuneBookTutorialCompleted)
        {
            enabled = false;
            return;
        }

        _stageManager = StageManager.Instance;
        _stageManager.StageStarted += HandleStageStarted;

        if (_stageManager.CurrentDepth == _targetDepth && _stageManager.Phase == StagePhase.Ready)
            BeginTutorial();
    }

    private void Update()
    {
        if (_isRunning)
            _activeCondition?.Tick();
    }

    private void OnDisable()
    {
        Release();
    }

    private void HandleStageStarted(int depth)
    {
        if (depth != _targetDepth || _isRunning)
            return;

        BeginTutorial();
    }

    private void HandlePhaseChanged(StagePhase phase)
    {
        if (phase != StagePhase.Ready)
            enabled = false;
    }

    private void BeginTutorial()
    {
        _isRunning = true;

        // 도착 연출을 기다리는 동안에도 자동 시작 카운트다운이 흐르지 않게 먼저 건다.
        _stageManager.IsAutoStartSuspended = true;
        _stageManager.PhaseChanged += HandlePhaseChanged;

        _runeBookPresenter = FindAnyObjectByType<RuneBookV2Presenter>(FindObjectsInactive.Include);
        _runeBookPresenter.SetAllowedEntryIds(new[] { _grantedRuneWordId });

        _showRoutine = StartCoroutine(ShowAfterArrivalRoutine());
    }

    private IEnumerator ShowAfterArrivalRoutine()
    {
        yield return null;

        while (PortalPlayerHider.IsAnyHiding || ScreenFader.IsCovering)
            yield return null;

        _showRoutine = null;

        // 층 시작 체크포인트가 저장된 뒤에 지급해야 이어하기마다 룬석이 쌓이지 않는다.
        GrantRuneStones();
        BeginStep(0);
    }

    private void GrantRuneStones()
    {
        if (!RuneEffectDatabase.Instance.TryGetById(_grantedRuneWordId, out RuneEffectDefinition effect))
            return;

        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        foreach (char runeCharacter in effect.RuneWord)
            runeManager.RuneStoneInventory.TryAddRuneStone(runeCharacter, 1);
    }

    private void BeginStep(int stepIndex)
    {
        _currentStepIndex = stepIndex;
        TutorialStepSO step = _steps[stepIndex];

        ITutorialCondition condition = TutorialConditionFactory.Create(step.Condition);
        _activeCondition = condition;

        _guideCardView.Show(step, stepIndex + 1, _steps.Length);
        if (step.Overlay == TutorialOverlayKind.KeyHint)
            _keyHintView.Show(step);

        condition.Completed += HandleConditionCompleted;
        condition.Begin(step);
    }

    private void HandleConditionCompleted()
    {
        EndActiveCondition();

        if (_currentStepIndex + 1 < _steps.Length)
            BeginStep(_currentStepIndex + 1);
        else
            Finish();
    }

    private void Finish()
    {
        _guideCardView.Hide();

        // 장착과 룬석 소모가 반영된 상태를 먼저 저장한다. 완료 기록만 남고 장착이 사라지는 일을 막는다.
        RunSaveController saveController = FindAnyObjectByType<RunSaveController>();
        if (saveController != null)
            saveController.SaveCheckpointNow();

        TutorialProgressStore.MarkRuneBookTutorialCompleted();
        enabled = false;
    }

    private void EndActiveCondition()
    {
        if (_activeCondition == null)
            return;

        _activeCondition.Completed -= HandleConditionCompleted;
        _activeCondition.End();
        _activeCondition = null;

        if (_keyHintView != null)
            _keyHintView.Hide();
    }

    private void Release()
    {
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        EndActiveCondition();

        if (_runeBookPresenter != null)
        {
            _runeBookPresenter.SetAllowedEntryIds(null);
            _runeBookPresenter = null;
        }

        if (_stageManager != null)
        {
            _stageManager.StageStarted -= HandleStageStarted;
            _stageManager.PhaseChanged -= HandlePhaseChanged;
            if (_isRunning)
                _stageManager.IsAutoStartSuspended = false;
        }

        if (_isRunning)
        {
            _isRunning = false;
            if (_guideCardView != null)
                _guideCardView.Hide();
        }
    }
}
