using System;
using System.Collections.Generic;

public class GameSession : Singleton<GameSession>
{
    // References
    private RuneEffectDatabase _runeEffectDatabase;
    private PlayerRuneManager _playerRuneManager;
    private RuneBook _runeBook;
    private IReadOnlyList<ResearchNodeDefinition> _researchDefinitions;

    private GameDefinitionSet _definitions;
    private bool _isSettlingProgression;

    public PlayerProgression Current { get; private set; }
    public RuneLoadout CurrentRuneLoadout { get; private set; }
    /// <summary>룬 워드 공개 상태 (도감)</summary>
    public RuneBook RuneBook => _runeBook;
    public bool IsReady { get; private set; }
    public event Action<PlayerProgression> ProgressReplaced;

    public void Initialize(RuneEffectDatabase database, PlayerRuneManager playerRuneManager,
        RuneBook book, IReadOnlyList<ResearchNodeDefinition> researchDefinitions, GameDefinitionSet definitions)
    {
        if (IsReady)
            throw new InvalidOperationException("GameSession already initialized");
        PlayerProgression progression = new(definitions);
        // 룬 문자 인벤토리는 PlayerRuneManager가 만든다. RuneLoadout보다 먼저 초기화한다.
        playerRuneManager.Initialize(database);
        // TODO: PlayerRuneManager 완성 시 RuneLoadout 제거
        RuneLoadout loadout = new(database, playerRuneManager.RuneStoneInventory, book, progression.ResearchTree,
            researchDefinitions);
        _runeEffectDatabase = database;
        _playerRuneManager = playerRuneManager;
        _runeBook = book;
        _researchDefinitions = new List<ResearchNodeDefinition>(researchDefinitions);
        _definitions = definitions;
        Current = progression;
        CurrentRuneLoadout = loadout;
        IsReady = true;
        // 룬북 공개 상태는 판과 상관없이 저장 파일에 남는다.
        SaveSystem.BindRuneBook(book);
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
        _playerRuneManager.ResetForNewGame();
        RuneLoadout loadout = new(_runeEffectDatabase, _playerRuneManager.RuneStoneInventory, _runeBook,
            progression.ResearchTree, _researchDefinitions);

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