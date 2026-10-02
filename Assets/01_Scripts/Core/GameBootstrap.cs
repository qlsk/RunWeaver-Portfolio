using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary> 기존 DB 초기화 후 장착 생성 의존성을 준비·전달하고 세션을 초기화한다. </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Definitions")]
    [SerializeField] private GoldDefinitionSO _gold;
    [SerializeField] private List<ResourceDefinitionSO> _resources;

    [Header("References")]
    [SerializeField] private GameSession _session;
    [SerializeField] private RuneStoneDatabase _runeStoneDatabase;
    [SerializeField] private RuneEffectDatabase _runeEffectDatabase;
    [SerializeField] private SkillDatabase _skillDatabase;
    [SerializeField] private ResearchDatabase _researchDatabase;
    [SerializeField] private RuneStoneInventory _runeStoneInventory;

    private BootstrapStage _currentStage;

    public BootstrapState State { get; private set; }
    public BootstrapStage FailedStage { get; private set; }

    private void Start()
    {
        InitializeAsync().Forget();
    }

    public async UniTask InitializeAsync()
    {
        if (State == BootstrapState.Initializing)
        {
            throw new InvalidOperationException($"{nameof(GameBootstrap)} is already initializing.");
        }

        try
        {
            State = BootstrapState.Initializing;
            _currentStage = BootstrapStage.ReferenceValidation;
            FailedStage = BootstrapStage.None;
            ValidateReferences();

            if (_session.IsReady)
            {
                State = BootstrapState.Ready;
                return;
            }

            _currentStage = BootstrapStage.RuneStoneDatabase;
            await _runeStoneDatabase.Initialize();

            _currentStage = BootstrapStage.RuneEffectDatabase;
            await _runeEffectDatabase.Initialize();

            _currentStage = BootstrapStage.SkillDatabase;
            await _skillDatabase.Initialize();

            _currentStage = BootstrapStage.ResearchDatabase;
            await _researchDatabase.Initialize();

            _currentStage = BootstrapStage.DefinitionSet;
            GameDefinitionSet definitionSet =
                new(_skillDatabase.SkillNodes, _researchDatabase.ResearchNodes, _gold, _resources);

            _currentStage = BootstrapStage.RuneBookPreparation;
            RuneBook runeBook = new(_runeEffectDatabase);
            runeBook.Initialize();

            _currentStage = BootstrapStage.RuneDependencyConfiguration;
            _session.ConfigureRuneDependencies(
                _runeEffectDatabase,
                _runeStoneInventory,
                runeBook,
                definitionSet.Researches);

            _currentStage = BootstrapStage.GameSession;
            _session.Initialize(definitionSet);

            State = BootstrapState.Ready;
        }
        catch (Exception exception)
        {
            FailedStage = _currentStage;
            State = BootstrapState.Failed;
            Debug.LogError($"Bootstrap failed on {FailedStage}: {exception.Message}, {exception.StackTrace}");
            ResetDatabases();
            throw;
        }
    }

    private void ValidateReferences()
    {
        Check(_gold, nameof(_gold));
        Check(_resources, nameof(_resources));
        Check(_session, nameof(_session));
        Check(_runeStoneDatabase, nameof(_runeStoneDatabase));
        Check(_runeEffectDatabase, nameof(_runeEffectDatabase));
        Check(_skillDatabase, nameof(_skillDatabase));
        Check(_researchDatabase, nameof(_researchDatabase));
        Check(_runeStoneInventory, nameof(_runeStoneInventory));
    }

    private void ResetDatabases()
    {
        _runeStoneDatabase?.ResetInitialization();
        _runeEffectDatabase?.ResetInitialization();
        _skillDatabase?.ResetInitialization();
        _researchDatabase?.ResetInitialization();
    }

    private static void Check(Object target, string name)
    {
        if (target == null)
        {
            throw new MissingReferenceException(name);
        }
    }

    private static void Check<T>(IReadOnlyList<T> targets, string name) where T : Object
    {
        if (targets == null)
        {
            throw new MissingReferenceException(name);
        }

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
            {
                throw new MissingReferenceException($"{name}[{i}]");
            }
        }
    }
}