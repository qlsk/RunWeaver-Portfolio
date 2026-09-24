using UnityEngine;

public class RuneWord
{
    private string _runeEffectId;
    private RuneWordRevealLevel _revealLevel;

    public string RuneEffectId => _runeEffectId;
    public RuneWordRevealLevel RevealLevel => _revealLevel;
    public bool IsRevealed => _revealLevel != RuneWordRevealLevel.Hidden;

    public RuneWord(string runeEffectId)
    {
        _runeEffectId = runeEffectId;
        _revealLevel = RuneWordRevealLevel.Hidden;
    }

    public bool TryReveal(RuneWordRevealLevel targetLevel)
    {
        if (_revealLevel == targetLevel)
        {
            Debug.LogWarning($"룬 효과 '{_runeEffectId}'의 공개 단계 변경 실패: 이미 '{targetLevel}' 단계입니다.");
            return false;
        }
        if (_revealLevel == RuneWordRevealLevel.Hidden && targetLevel == RuneWordRevealLevel.Detailed)
        {
            Debug.LogWarning($"룬 효과 '{_runeEffectId}'의 상세 정보 공개 실패: 숨겨진 상태입니다. 먼저 목록에 공개해야 합니다.");
            return false;
        }
        if ((int)_revealLevel > (int)targetLevel)
        {
            Debug.LogWarning($"룬 효과 '{_runeEffectId}'의 공개 단계 변경 실패: '{_revealLevel}'에서 '{targetLevel}'로 단계를 낮출 수 없습니다.");
            return false;
        }
        _revealLevel = targetLevel;
        return true;
    }
}
