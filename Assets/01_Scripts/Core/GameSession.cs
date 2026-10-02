using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameSession : Singleton<GameSession>
{
    private GameDefinitionSet _definitions;
    private PlayerProgression _current;
    private bool _isReady;
    private bool _isSettlingProgression;

    public PlayerProgression Current => _current;
    public bool IsReady => _isReady;
    public event Action<PlayerProgression> ProgressReplaced;

    public void Initialize(GameDefinitionSet definitions)
    {
        if (_isReady)
        {
            throw new InvalidOperationException("GameSession already initialized");
        }
        if (definitions == null)
        {
            throw new ArgumentNullException(nameof(definitions));
        }

        PlayerProgression progression = new(definitions);
        _definitions = definitions;
        _current = progression;
        _isReady = true;
        SafeEventNotifier.Notify(ProgressReplaced, progression);
    }

    public void StartNewGame()
    {
        if (!_isReady)
        {
            throw new InvalidOperationException("GameSession is not ready");
        }
        if (_isSettlingProgression)
        {
            throw new InvalidOperationException("Cannot start a new game during settlement.");
        }
        if (_current.Holdings.IsOnExpedition)
        {
            throw new InvalidOperationException("End the expedition before starting a new game.");
        }

        PlayerProgression progression = new(_definitions);
        _current = progression;
        SafeEventNotifier.Notify(ProgressReplaced, progression);
    }

    public void StartExpedition()
    {
        if (!_isReady)
        {
            throw new InvalidOperationException("GameSession is not ready");
        }
        if (_isSettlingProgression)
        {
            throw new InvalidOperationException("Cannot start an expedition during settlement.");
        }
        if (_current.Holdings.IsOnExpedition)
        {
            throw new InvalidOperationException("Expedition already in progress.");
        }

        Wallet wallet = new(_definitions.Gold);
        ResourceInventory resourceInventory = new(_definitions.Resources);
        _current.Holdings.AttachExpeditionData(wallet, resourceInventory);
    }

    public void CompleteReturn()
    {
        EnsureCanSettle();

        try
        {
            _isSettlingProgression = true;
            IReadOnlyDictionary<string, int> expeditionResources = _current.Holdings.ExpeditionResources.GetSnapshot();
            _current.Holdings.StoredResources.AddResources(expeditionResources);
            _current.Holdings.DetachExpeditionData();
        }
        finally
        {
            _isSettlingProgression = false;
        }
    }

    public IReadOnlyDictionary<string, int> CompleteFailure(IReadOnlyDictionary<string, int> resourcesForRecovery)
    {
        EnsureCanSettle();

        try
        {
            _isSettlingProgression = true;
            IReadOnlyDictionary<string, int> resourcesForRecoveryCopy =
                new Dictionary<string, int>(resourcesForRecovery);
            IReadOnlyDictionary<string, int> expeditionResources = _current.Holdings.ExpeditionResources.GetSnapshot();
            foreach ((string resourceId, int recoveryAmount) in resourcesForRecoveryCopy)
            {
                if (!expeditionResources.TryGetValue(resourceId, out int amount))
                {
                    throw new KeyNotFoundException($"Unknown resource ID: {resourceId}");
                }

                if (recoveryAmount < 0 || recoveryAmount > amount)
                {
                    throw new ArgumentOutOfRangeException(nameof(resourcesForRecoveryCopy));
                }
            }

            _current.Holdings.DetachExpeditionData();
            return resourcesForRecoveryCopy;
        }
        finally
        {
            _isSettlingProgression = false;
        }
    }

    private void EnsureCanSettle()
    {
        if (!_isReady)
        {
            throw new InvalidOperationException("GameSession is not ready.");
        }
        if (_isSettlingProgression)
        {
            throw new InvalidOperationException("Settlement in progress.");
        }
        if (!_current.Holdings.IsOnExpedition)
        {
            throw new InvalidOperationException("No expedition in progress.");
        }
    }
}