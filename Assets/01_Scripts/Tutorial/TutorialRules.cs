using UnityEngine;

/// <summary>튜토리얼 진행 중에만 적용하는 규칙을 켜고 끈다. 지금은 룬북(Tab) 잠금만 다룬다.</summary>
public class TutorialRules
{
    private RuneBookV2Toggle _runeBookToggle;

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

    /// <summary>단계가 시작될 때 단계별 규칙을 갱신한다. 룬 장착 단계가 시작되면 룬북 잠금을 푼다.</summary>
    public void OnStepStarted(TutorialStepSO step)
    {
        if (step.Condition == TutorialConditionKind.RuneConfirm)
            Release();
    }

    /// <summary>적용한 규칙을 되돌린다. 여러 번 불러도 안전하다.</summary>
    public void Release()
    {
        if (_runeBookToggle != null)
            _runeBookToggle.IsLocked = false;

        _runeBookToggle = null;
    }
}
