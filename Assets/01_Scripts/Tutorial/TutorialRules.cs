using System.Collections.Generic;
using UnityEngine;

/// <summary>튜토리얼 진행 중에만 적용하는 규칙을 켜고 끈다. 룬북(Tab) 잠금, 룬석 지급, 1층 미션 고정, 임무 실패 없음(체력 하한과 제한 시간 연장)을 다룬다.</summary>
public class TutorialRules
{
    private readonly IReadOnlyList<string> _grantedRuneWordIds;
    private readonly float _minHealthRatio;
    private readonly float _timeoutExtendSeconds;

    private RuneBookV2Toggle _runeBookToggle;
    private MissionManager _missionManager;
    private PlayerHealth _playerHealth;
    private StageManager _stageManager;
    private bool _runeStonesGranted;

    public TutorialRules(IReadOnlyList<string> grantedRuneWordIds, float minHealthRatio, float timeoutExtendSeconds)
    {
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

    /// <summary>단계가 시작될 때 단계별 규칙을 갱신한다. 룬 장착 단계가 시작되면 룬석을 주고 룬북 잠금을 푼다.</summary>
    public void OnStepStarted(TutorialStepSO step)
    {
        if (step.Condition != TutorialConditionKind.RuneConfirm)
            return;

        GrantRuneStones();
        ReleaseRuneBookLock();
    }

    /// <summary>적용한 규칙을 되돌린다. 지급한 룬석은 되돌리지 않는다. 여러 번 불러도 안전하다.</summary>
    public void Release()
    {
        ReleaseRuneBookLock();
        ReleaseMissionFix();
        ReleaseNoFailure();
    }

    private void LockRuneBook()
    {
        _runeBookToggle = UnityEngine.Object.FindAnyObjectByType<RuneBookV2Toggle>(FindObjectsInactive.Include);
        if (_runeBookToggle == null)
        {
            Debug.LogWarning("[TutorialRules] RuneBookV2Toggle을 찾을 수 없어 룬북을 잠그지 못합니다.");
            return;
        }

        _runeBookToggle.IsLocked = true;
    }

    private void ReleaseRuneBookLock()
    {
        if (_runeBookToggle != null)
            _runeBookToggle.IsLocked = false;

        _runeBookToggle = null;
    }

    /// <summary>1층 미션을 영혼 수집으로 고정한다. 시작 석상이 1층을 시작할 때 추첨에 쓰인다.</summary>
    private void FixFirstMission()
    {
        // MissionManager.Instance는 없으면 새로 만들므로, 생성 없이 기존 인스턴스만 찾는다.
        _missionManager = UnityEngine.Object.FindAnyObjectByType<MissionManager>();
        if (_missionManager == null)
        {
            Debug.LogWarning("[TutorialRules] MissionManager를 찾을 수 없어 1층 미션을 고정하지 못합니다.");
            return;
        }

        _missionManager.SetNextMissionOverride(MissionType.SoulCollect);
    }

    /// <summary>아직 추첨에 쓰이지 않은 미션 고정을 풀어 튜토리얼 밖의 추첨에 영향을 주지 않게 한다.</summary>
    private void ReleaseMissionFix()
    {
        if (_missionManager != null)
            _missionManager.ClearNextMissionOverride();

        _missionManager = null;
    }

    /// <summary>임무 실패 없음: 체력 하한을 걸고, 제한 시간이 끝나면 실패 대신 시간을 늘린다.</summary>
    private void ApplyNoFailure()
    {
        _playerHealth = UnityEngine.Object.FindAnyObjectByType<PlayerHealth>();
        if (_playerHealth != null)
            _playerHealth.MinHealthRatio = _minHealthRatio;
        else
            Debug.LogWarning("[TutorialRules] PlayerHealth를 찾을 수 없어 체력 하한을 걸지 못합니다.");

        // StageManager.Instance는 없으면 새로 만들거나 예외를 던지므로, 생성 없이 기존 인스턴스만 찾는다.
        _stageManager = UnityEngine.Object.FindAnyObjectByType<StageManager>();
        if (_stageManager != null)
            _stageManager.MissionTimeoutHandler = HandleMissionTimeout;
        else
            Debug.LogWarning("[TutorialRules] StageManager를 찾을 수 없어 제한 시간 초과 처리를 걸지 못합니다.");
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

    /// <summary>제한 시간이 끝나면 실패 대신 남은 시간을 늘린다.</summary>
    private bool HandleMissionTimeout()
    {
        if (_stageManager == null)
            return false;

        _stageManager.ExtendMissionTime(_timeoutExtendSeconds);
        Debug.Log($"[TutorialRules] 제한 시간이 끝나 {_timeoutExtendSeconds}초를 연장했습니다.");
        return true;
    }

    /// <summary>허용 룬 워드마다 룬 문자를 한 번씩 지급한다. 같은 문자가 여러 단어에 있으면 그 수만큼 쌓인다.</summary>
    private void GrantRuneStones()
    {
        if (_runeStonesGranted || _grantedRuneWordIds == null)
            return;

        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        RuneEffectDatabase effectDatabase = RuneEffectDatabase.Instance;
        if (runeManager == null || !runeManager.IsInitialized || effectDatabase == null || !effectDatabase.DatabaseReady)
        {
            Debug.LogWarning("[TutorialRules] 룬 시스템이 준비되지 않아 룬석을 지급하지 못합니다.");
            return;
        }

        _runeStonesGranted = true;
        foreach (string runeWordId in _grantedRuneWordIds)
        {
            if (string.IsNullOrEmpty(runeWordId))
            {
                Debug.LogWarning("[TutorialRules] 룬석 지급 목록에 비어 있는 id가 있어 건너뜁니다.");
                continue;
            }

            if (!effectDatabase.TryGetById(runeWordId, out RuneEffectDefinition effect))
            {
                Debug.LogWarning($"[TutorialRules] 룬 효과 '{runeWordId}'를 찾을 수 없어 룬석을 지급하지 못합니다.");
                continue;
            }

            foreach (char runeCharacter in effect.RuneWord)
                runeManager.RuneStoneInventory.TryAddRuneStone(runeCharacter, 1);
        }
    }
}
