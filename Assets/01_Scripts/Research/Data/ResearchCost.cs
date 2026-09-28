using System;

[Serializable]
public struct ResearchCost
{
    public string ResourceId;
    public int Amount;

    public ResearchCost(string resourceId, int amount)
    {
        ResourceId = resourceId;
        Amount = amount;
    }
}
