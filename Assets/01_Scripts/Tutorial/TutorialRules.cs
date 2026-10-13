using System.Collections.Generic;
using UnityEngine;

/// <summary>튜토리얼 진행 중에만 적용하는 규칙을 켜고 끈다. 룬북(Tab) 잠금, 룬석 지급, 1층 미션 고정을 다룬다.</summary>
public class TutorialRules
{
    private readonly IReadOnlyList<string> _grantedRuneWordIds;

    private RuneBookV2Toggle _runeBookToggle;
    private MissionManager _missionManager;
    private bool _runeStonesGranted;

    public TutorialRules(IReadOnlyList<string> grantedRuneWordIds)
    {
        _grantedRuneWordIds = grantedRuneWordIds;
    }

    public void Apply()
    {
        LockRuneBook();
        FixFirstMission();
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
