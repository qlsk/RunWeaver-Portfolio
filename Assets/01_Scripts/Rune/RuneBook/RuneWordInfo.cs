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
    private bool _areDetailsRevealed;

    public string RuneEffectId => _runeEffectId;
    public RuneEffectCategory Category => _category;
    public RuneWordRevealLevel RevealLevel => _revealLevel;
    public string DisplayName => _displayName;
    public string RuneWord => _runeWord;
    public string Description => _description;
    public Sprite Icon => _icon;
    public bool AreDetailsRevealed => _areDetailsRevealed;

    public RuneWordInfo(RuneEffectDefinition definition, RuneWordRevealLevel revealLevel)
    {
        _runeEffectId = definition.Id;
        _category = definition.Category;
        _revealLevel = revealLevel;
        _displayName = definition.DisplayName;
        _runeWord = definition.RuneWord;
        _description = definition.Description;
        _icon = definition.Icon;
        _areDetailsRevealed = revealLevel == RuneWordRevealLevel.Detailed;
    }
}
