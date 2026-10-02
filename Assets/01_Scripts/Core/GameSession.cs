using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameSession : Singleton<GameSession>
{
    private RuneEffectDatabase _runeEffectDatabase;
    private RuneStoneInventory _runeStoneInventory;
    private RuneBook _runeBook;
    private IReadOnlyList<ResearchNodeDefinition> _researchDefinitions;
    private bool _runeDependenciesConfigured;

    private GameDefinitionSet _definitions;
    private bool _isSettlingProgression;

    public PlayerProgression Current { get; private set; }
    public RuneLoadout CurrentRuneLoadout { get; private set; }
    public bool IsReady { get; private set; }
    public event Action<PlayerProgression> ProgressReplaced;

    public void ConfigureRuneDependencies(
        RuneEffectDatabase database,
        RuneStoneInventory inventory,
        RuneBook book,
        IReadOnlyList<ResearchNodeDefinition> researchDefinitions)
    {
        // 초기 Initialize 전에 준비된 의존성을 전달·검증한다.
        if (IsReady)
            throw new InvalidOperationException("GameSession already initialized");

        if (database == null)
            throw new ArgumentNullException(nameof(database));
        if (!database.DatabaseReady)
            throw new InvalidOperationException("[GameSession.ConfigureRuneDependencies] Database is not ready.");
        if (inventory == null)
            throw new ArgumentNullException(nameof(inventory));
        if (book == null)
            throw new ArgumentNullException(nameof(book));
        if (!book.IsInitialized)
            throw new InvalidOperationException("[GameSession.ConfigureRuneDependencies] Book is not initialized.");
        if (researchDefinitions == null)
            throw new ArgumentNullException(nameof(researchDefinitions));

        _runeEffectDatabase = database;
        _runeStoneInventory = inventory;
        _runeBook = book;
        _researchDefinitions = new List<ResearchNodeDefinition>(researchDefinitions);

        _runeDependenciesConfigured = true;
    }

    public void ResetRuneDependencies()
    {
        // 초기 세션 게시 전 실패 시 복구한다.
        if (IsReady)
            throw new InvalidOperationException("GameSession already initialized");

        _runeEffectDatabase = null;
        _runeStoneInventory = null;
        _runeBook = null;
        _researchDefinitions = null;

        _runeDependenciesConfigured = false;
    }

    public void Initialize(GameDefinitionSet definitions)
    {
        if (IsReady)
            throw new InvalidOperationException("[GameSession.Initialize] GameSession already initialized");
        if (!_runeDependenciesConfigured)
            throw new InvalidOperationException("[GameSession.Initialize] RuneDependencies is not configured.");
        if (_researchDefinitions.Any(definition => !definitions.Researches.Contains(definition)))
            throw new InvalidOperationException("[GameSession.Initialize] Research definition is not registered.");

        PlayerProgression progression = new(definitions);

        RuneLoadout loadout = new(
            _runeEffectDatabase,
            _runeStoneInventory,
            _runeBook,
            progression.ResearchTree,
            _researchDefinitions);

        _definitions = definitions;
        Current = progression;
        CurrentRuneLoadout = loadout;

        IsReady = true;
        SafeEventNotifier.Notify(ProgressReplaced, progression);
    }

    public void StartNewGame()
    {
        if (!IsReady)
            throw new InvalidOperationException("GameSession is not ready");
        if (_isSettlingProgression)
            throw new InvalidOperationException("Cannot start a new game during settlement.");
        if (Current.Holdings.IsOnExpedition)
            throw new InvalidOperationException("End the expedition before starting a new game.");

        PlayerProgression progression = new(_definitions);
        RuneLoadout loadout = new(
            _runeEffectDatabase,
            _runeStoneInventory,
            _runeBook,
            progression.ResearchTree,
            _researchDefinitions);

        Current = progression;
        CurrentRuneLoadout = loadout;

        SafeEventNotifier.Notify(ProgressReplaced, progression);
    }

    public void StartExpedition()
    {
        if (!IsReady)
            throw new InvalidOperationException("GameSession is not ready");
        if (_isSettlingProgression)
            throw new InvalidOperationException("Cannot start an expedition during settlement.");
        if (Current.Holdings.IsOnExpedition)
            throw new InvalidOperationException("Expedition already in progress.");

        Wallet wallet = new(_definitions.Gold);
        ResourceInventory resourceInventory = new(_definitions.Resources);
        Current.Holdings.AttachExpeditionData(wallet, resourceInventory);
    }

    public void CompleteReturn()
    {
        EnsureCanSettle();

        try
        {
            _isSettlingProgression = true;
            IReadOnlyDictionary<string, int> expeditionResources = Current.Holdings.ExpeditionResources.GetSnapshot();
            Current.Holdings.StoredResources.AddResources(expeditionResources);
            Current.Holdings.DetachExpeditionData();
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
            IReadOnlyDictionary<string, int> expeditionResources = Current.Holdings.ExpeditionResources.GetSnapshot();
            foreach ((string resourceId, int recoveryAmount) in resourcesForRecoveryCopy)
            {
                if (!expeditionResources.TryGetValue(resourceId, out int amount))
                    throw new KeyNotFoundException($"Unknown resource ID: {resourceId}");

                if (recoveryAmount < 0 || recoveryAmount > amount)
                    throw new ArgumentOutOfRangeException(nameof(resourcesForRecoveryCopy));
            }

            Current.Holdings.DetachExpeditionData();
            return resourcesForRecoveryCopy;
        }
        finally
        {
            _isSettlingProgression = false;
        }
    }

    private void EnsureCanSettle()
    {
        if (!IsReady)
            throw new InvalidOperationException("GameSession is not ready.");
        if (_isSettlingProgression)
            throw new InvalidOperationException("Settlement in progress.");
        if (!Current.Holdings.IsOnExpedition)
            throw new InvalidOperationException("No expedition in progress.");
    }
}
