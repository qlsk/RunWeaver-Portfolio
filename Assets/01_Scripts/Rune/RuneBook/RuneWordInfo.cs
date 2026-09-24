using UnityEngine;

public class RuneWordInfo
{
    private string _runeEffectId;
    private RuneEffectCategory _category;
    private RuneWordRevealLevel _revealLevel;
    private string _displayName;
    private string _runeWord;
    private string _description;
    private Sprite _icon;
    private bool _isRevealed;
    private bool _areDetailsRevealed;

    public string RuneEffectId => _runeEffectId;
    public RuneEffectCategory Category => _category;
    public RuneWordRevealLevel RevealLevel => _revealLevel;
    public string DisplayName => _displayName;
    public string RuneWord => _runeWord;
    public string Description => _description;
    public Sprite Icon => _icon;
    public bool IsRevealed => _isRevealed;
    public bool AreDetailsRevealed => _areDetailsRevealed;

    public RuneWordInfo(RuneEffectData data, RuneWordRevealLevel revealLevel)
    {
        _runeEffectId = data.Id;
        _category = data.Category;
        _revealLevel = revealLevel;
        _displayName = data.DisplayName;
        _runeWord = data.RuneWord;
        _description = data.Description;
        _icon = data.Icon;
        _isRevealed = revealLevel != RuneWordRevealLevel.Hidden;
        _areDetailsRevealed = revealLevel == RuneWordRevealLevel.Detailed;
    }
}
