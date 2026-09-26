public class GoldDefinition
{
    private string _id;
    private string _displayName;

    public string Id => _id;
    public string DisplayName => _displayName;

    public GoldDefinition(string id, string displayName)
    {
        _id = id;
        _displayName = displayName;
    }
}
