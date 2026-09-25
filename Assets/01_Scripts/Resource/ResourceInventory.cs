using System;
using System.Collections.Generic;

public class ResourceInventory
{
    private Dictionary<string, ResourceData> _resources;
    private Dictionary<string, int> _quantities;

    public event Action QuantitiesChanged;

    public ResourceInventory(IReadOnlyList<ResourceData> resources)
    {
        _resources = new Dictionary<string, ResourceData>();
        _quantities = new Dictionary<string, int>();
        foreach (ResourceData resource in resources)
        {
            if (!_resources.TryAdd(resource.Id, resource) || !_quantities.TryAdd(resource.Id, 0))
            {
                throw new InvalidOperationException($"Duplicate resource ID: {resource.Id}");
            }
        }
    }

    public int GetQuantity(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new ArgumentException("Resource ID is required.", nameof(resourceId));
        }
        if (!_resources.ContainsKey(resourceId))
        {
            throw new KeyNotFoundException($"Resource not found: {resourceId}");
        }
        return _quantities[resourceId];
    }

    public void AddResource(string resourceId, int amount)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new ArgumentException("Resource ID is required.", nameof(resourceId));
        }
        if (!_quantities.ContainsKey(resourceId))
        {
            throw new KeyNotFoundException($"Resource not found: {resourceId}");
        }
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
        }
        _quantities[resourceId] += amount;
        QuantitiesChanged?.Invoke();
    }

    public bool TrySpend(IReadOnlyList<ResourceCost> costs)
    {
        Dictionary<string, int> costSums = new();
        foreach (ResourceCost cost in costs)
        {
            if (cost.Amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), cost.Amount,
                    "Cost cannot be negative.");
            }
            costSums.TryAdd(cost.ResourceId, 0);
            costSums[cost.ResourceId] += cost.Amount;
        }
        foreach ((string resourceId, int cost) in costSums)
        {
            if (!_quantities.TryGetValue(resourceId, out int quantity))
            {
                throw new KeyNotFoundException($"Resource not found: {resourceId}");
            }
            if (quantity < cost) return false;
        }
        foreach ((string resourceId, int cost) in costSums)
        {
            _quantities[resourceId] -= cost;
        }
        QuantitiesChanged?.Invoke();
        return true;
    }
}
