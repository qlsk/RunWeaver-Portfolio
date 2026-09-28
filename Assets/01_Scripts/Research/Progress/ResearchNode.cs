public class ResearchNode
{
    private string _researchId;
    private bool _isUnlocked;
    private bool _isActive;

    public string ResearchId => _researchId;
    public bool IsUnlocked => _isUnlocked;
    public bool IsActive => _isActive;

    public ResearchNode(string researchId)
    {
        _researchId = researchId;
        _isUnlocked = false;
    }

    public bool TryUnlock()
    {
        bool previousState = _isUnlocked;
        _isUnlocked = true;
        return !previousState;
    }

    public bool TryActivate()
    {
        if (!_isUnlocked) return false;
        bool previousState = _isActive;
        _isActive = true;
        return !previousState;
    }
}
