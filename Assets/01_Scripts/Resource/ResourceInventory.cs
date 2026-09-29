using System;
using System.Collections.Generic;

public class ResourceInventory
{
    private readonly Dictionary<string, int> _quantities;

    public event Action QuantitiesChanged;

    public ResourceInventory(IReadOnlyList<ResourceDefinitionSO> resources)
    {
        if (resources == null)
        {
            throw new ArgumentNullException(nameof(resources));
        }
        _quantities = new Dictionary<string, int>();
        foreach (ResourceDefinitionSO resource in resources)
        {
            if (resource == null)
            {
                throw new ArgumentException("Resources cannot contain null elements.", nameof(resources));
            }
            if (string.IsNullOrWhiteSpace(resource.Id))
            {
                throw new ArgumentException("A resource ID cannot be null, empty, or whitespace.", nameof(resources));
            }
            if (!_quantities.TryAdd(resource.Id, 0))
            {
                throw new ArgumentException($"Duplicate resource ID: {resource.Id}", nameof(resources));
            }
        }
    }

    public int GetQuantity(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new ArgumentException("Resource ID is required.", nameof(resourceId));
        }
        if (!_quantities.TryGetValue(resourceId, out int quantity))
        {
            throw new KeyNotFoundException($"Resource not found: {resourceId}");
        }
        return quantity;
    }

    public IReadOnlyDictionary<string, int> GetSnapshot()
    {
        return new Dictionary<string, int>(_quantities);
    }

    public void AddResources(IReadOnlyDictionary<string, int> amounts)
    {
        IReadOnlyDictionary<string, int> amountsCopy = new Dictionary<string, int>(amounts);
        foreach ((string resourceId, int amount) in amountsCopy)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ArgumentException("Resource ID cannot be null, empty, or whitespace.", nameof(amounts));
            }
            if (!_quantities.ContainsKey(resourceId))
            {
                throw new KeyNotFoundException($"Resource not found: {resourceId}");
            }
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, $"Amount must be positive: {resourceId}");
            }
            _ = checked(_quantities[resourceId] + amount);
        }
        foreach ((string resourceId, int amount) in amountsCopy)
        {
            _quantities[resourceId] += amount;
        }
        SafeEventNotifier.Notify(QuantitiesChanged);
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
        SafeEventNotifier.Notify(QuantitiesChanged);
    }

    public bool TrySpend(IReadOnlyList<ResourceCost> costs)
    {
        Dictionary<string, int> costSums = new();
        foreach (ResourceCost cost in costs)
        {
            if (cost.Amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), cost.Amount, "Cost cannot be negative.");
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
        SafeEventNotifier.Notify(QuantitiesChanged);
        return true;
    }
}
