public class GoldData
{
    private string _id;
    private string _displayName;

    public string Id => _id;
    public string DisplayName => _displayName;

    public GoldData(string id, string displayName)
    {
        _id = id;
        _displayName = displayName;
    }
}
