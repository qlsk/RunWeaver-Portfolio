using System.Collections.Generic;
using UnityEngine;

/// <summary>튜토리얼 진행 중에만 적용하는 규칙을 켜고 끈다. 룬북(Tab) 잠금과 룬석 지급을 다룬다.</summary>
public class TutorialRules
{
    private readonly IReadOnlyList<string> _grantedRuneWordIds;

    private RuneBookV2Toggle _runeBookToggle;
    private bool _runeStonesGranted;

    public TutorialRules(IReadOnlyList<string> grantedRuneWordIds)
    {
        _grantedRuneWordIds = grantedRuneWordIds;
    }

    public void Apply()
    {
        _runeBookToggle = UnityEngine.Object.FindAnyObjectByType<RuneBookV2Toggle>(FindObjectsInactive.Include);
        if (_runeBookToggle == null)
        {
            Debug.LogWarning("[TutorialRules] RuneBookV2Toggle을 찾을 수 없어 룬북을 잠그지 못합니다.");
            return;
        }

        _runeBookToggle.IsLocked = true;
    }

    /// <summary>단계가 시작될 때 단계별 규칙을 갱신한다. 룬 장착 단계가 시작되면 룬석을 주고 룬북 잠금을 푼다.</summary>
    public void OnStepStarted(TutorialStepSO step)
    {
        if (step.Condition != TutorialConditionKind.RuneConfirm)
            return;

        GrantRuneStones();
        Release();
    }

    /// <summary>적용한 규칙을 되돌린다. 지급한 룬석은 되돌리지 않는다. 여러 번 불러도 안전하다.</summary>
    public void Release()
    {
        if (_runeBookToggle != null)
            _runeBookToggle.IsLocked = false;

        _runeBookToggle = null;
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
