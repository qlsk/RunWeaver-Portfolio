using System.Collections.Generic;
using UnityEngine;

public class TutorialRules
{
    private readonly IReadOnlyList<string> _grantedRuneWordIds;
    private readonly float _minHealthRatio;
    private readonly float _timeoutExtendSeconds;
    private readonly TutorialRecordTracker _recordTracker;

    private RuneBookV2Toggle _runeBookToggle;
    private MissionManager _missionManager;
    private RuneBookV2Presenter _runeBookPresenter;
    private PlayerHealth _playerHealth;
    private StageManager _stageManager;
    private bool _runeStonesGranted;

    public TutorialRules(
        IReadOnlyList<string> grantedRuneWordIds,
        float minHealthRatio,
        float timeoutExtendSeconds,
        TutorialRecordTracker recordTracker)
    {
        _recordTracker = recordTracker;
        _grantedRuneWordIds = grantedRuneWordIds;
        _minHealthRatio = minHealthRatio;
        _timeoutExtendSeconds = timeoutExtendSeconds;
    }

    public void Apply()
    {
        LockRuneBook();
        FixFirstMission();
        ApplyNoFailure();
    }

    public void OnStepStarted(TutorialStepSO step)
    {
        if (step.Condition != TutorialConditionKind.RuneConfirm)
            return;

        GrantRuneStones();
        RestrictRuneWords();
        ReleaseRuneBookLock();
    }

    public void Release()
    {
        ReleaseRuneBookLock();
        ReleaseMissionFix();
        ReleaseNoFailure();
        ReleaseRuneWordRestriction();
    }

    private void LockRuneBook()
    {
        _runeBookToggle = Object.FindAnyObjectByType<RuneBookV2Toggle>(FindObjectsInactive.Include);
        _runeBookToggle.IsLocked = true;
    }

    private void ReleaseRuneBookLock()
    {
        if (_runeBookToggle != null)
            _runeBookToggle.IsLocked = false;

        _runeBookToggle = null;
    }

    private void FixFirstMission()
    {
        _missionManager = Object.FindAnyObjectByType<MissionManager>();
        _missionManager.SetNextMissionOverride(MissionType.SoulCollect);
    }

    private void ReleaseMissionFix()
    {
        if (_missionManager != null)
            _missionManager.ClearNextMissionOverride();

        _missionManager = null;
    }

    private void RestrictRuneWords()
    {
        _runeBookPresenter = Object.FindAnyObjectByType<RuneBookV2Presenter>(FindObjectsInactive.Include);
        _runeBookPresenter.SetAllowedEntryIds(_grantedRuneWordIds);
    }

    private void ReleaseRuneWordRestriction()
    {
        if (_runeBookPresenter != null)
            _runeBookPresenter.SetAllowedEntryIds(null);

        _runeBookPresenter = null;
    }

    private void ApplyNoFailure()
    {
        _playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        _playerHealth.MinHealthRatio = _minHealthRatio;

        _stageManager = Object.FindAnyObjectByType<StageManager>();
        _stageManager.MissionTimeoutHandler = HandleMissionTimeout;
    }

    private void ReleaseNoFailure()
    {
        if (_playerHealth != null)
            _playerHealth.MinHealthRatio = 0f;

        if (_stageManager != null)
            _stageManager.MissionTimeoutHandler = null;

        _playerHealth = null;
        _stageManager = null;
    }

    private bool HandleMissionTimeout()
    {
        if (_stageManager == null)
            return false;

        _stageManager.ExtendMissionTime(_timeoutExtendSeconds);

        if (_recordTracker != null)
            _recordTracker.Notify(RecordTriggerKind.TimeExtended);
        return true;
    }

    private void GrantRuneStones()
    {
        if (_runeStonesGranted || _grantedRuneWordIds == null)
            return;

        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        RuneEffectDatabase effectDatabase = RuneEffectDatabase.Instance;

        _runeStonesGranted = true;
        foreach (string runeWordId in _grantedRuneWordIds)
        {
            if (!effectDatabase.TryGetById(runeWordId, out RuneEffectDefinition effect))
                continue;

            foreach (char runeCharacter in effect.RuneWord)
                runeManager.RuneStoneInventory.TryAddRuneStone(runeCharacter, 1);
        }
    }
}