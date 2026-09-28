public class ResourceCost
{
    private string _resourceId;
    private int _amount;

    public string ResourceId => _resourceId;
    public int Amount => _amount;

    public ResourceCost(string resourceId, int amount)
    {
        _resourceId = resourceId;
        _amount = amount;
    }
}
