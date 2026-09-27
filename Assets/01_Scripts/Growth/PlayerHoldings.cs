using System;
using System.Collections.Generic;

public class PlayerHoldings
{
    private Wallet _expeditionWallet;
    private readonly ResourceInventory _storedResources;
    private ResourceInventory _expeditionResources;
    private bool _isOnExpedition;

    public Wallet ExpeditionWallet => _expeditionWallet;
    public ResourceInventory StoredResources => _storedResources;
    public ResourceInventory ExpeditionResources => _expeditionResources;
    public bool IsOnExpedition => _isOnExpedition;
    public event Action ExpeditionChanged;

    public PlayerHoldings(IReadOnlyList<ResourceDefinitionSO> resources)
    {
        if (resources == null)
        {
            throw new ArgumentNullException(nameof(resources));
        }
        _storedResources = new ResourceInventory(resources);
    }

    public void AttachExpeditionData(Wallet wallet, ResourceInventory resources)
    {
        if (wallet == null)
        {
            throw new ArgumentNullException(nameof(wallet));
        }
        if (resources == null)
        {
            throw new ArgumentNullException(nameof(resources));
        }
        if (resources == _storedResources)
        {
            throw new ArgumentException("The stored resources cannot be the expedition resources", nameof(resources));
        }
        if (IsOnExpedition)
        {
            throw new InvalidOperationException("The expedition is already started");
        }
        _expeditionWallet = wallet;
        _expeditionResources = resources;
        _isOnExpedition = true;
        ExpeditionChanged?.Invoke();
    }

    public void DetachExpeditionData()
    {
        if (!IsOnExpedition) return;
        _expeditionWallet = null;
        _expeditionResources = null;
        _isOnExpedition = false;
        ExpeditionChanged?.Invoke();
    }
}
