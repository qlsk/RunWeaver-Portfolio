public class ResourceDefinition
{
    string _id;
    string _displayName;
    string _description;

    public string Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;

    public ResourceDefinition(string id, string displayName, string description)
    {
        _id = id;
        _displayName = displayName;
        _description = description;
    }
}
