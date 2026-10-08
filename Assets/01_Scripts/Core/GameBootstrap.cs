using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// 기존 DB 초기화 후 장착 생성 의존성을 준비·전달하고 세션을 초기화한다.
public class GameBootstrap : MonoBehaviour
{
    private enum CsvLoadPolicy
    {
        PreferCache,
        RefreshFromRemote
    }

    [Header("CSV 캐시")]
    [Tooltip("PreferCache: 캐시 우선 사용. RefreshFromRemote: 새 실행에서 시트 전체를 다운로드하고 캐시 갱신.")]
    [SerializeField] private CsvLoadPolicy _csvLoadPolicy = CsvLoadPolicy.PreferCache;

    [Header("Definitions")]
    [SerializeField] private GoldDefinitionSO _gold;
    [SerializeField] private List<ResourceDefinitionSO> _resources;

    [Header("Databases")]
    [SerializeField] private RuneStoneDatabase _runeStoneDatabase;
    [SerializeField] private RuneEffectDatabase _runeEffectDatabase;
    [SerializeField] private SkillDatabase _skillDatabase;
    [SerializeField] private ResearchDatabase _researchDatabase;

    [Header("Session References")]
    [SerializeField] private GameSession _session;
    [SerializeField] private PlayerRuneManager _playerRuneManager;

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

        bool ownsCsvInitialization = false;
        try
        {
            State = BootstrapState.Initializing;
            _currentStage = BootstrapStage.ReferenceValidation;
            FailedStage = BootstrapStage.None;
            ValidateReferences();

            // 두 번째 씬부터는 이 씬의 GameSession이 중복으로 파괴되므로, 이미 살아 있는 세션(Instance)으로 확인한다.
            GameSession activeSession = GameSession.Instance != null ? GameSession.Instance : _session;
            if (activeSession.IsReady)
            {
                State = BootstrapState.Ready;
                return;
            }

            CsvCacheStore cacheStore = new(Path.Combine(Application.persistentDataPath, "DefinitionCache"));
            bool useCache = CsvLoader.BeginInitialization(cacheStore, _csvLoadPolicy == CsvLoadPolicy.PreferCache);
            ownsCsvInitialization = true;
            (GameDefinitionSet definitionSet, RuneBook runeBook) prepared;
            try
            {
                prepared = await PrepareDefinitions();
            }
            catch (Exception exception) when (useCache && IsCacheDataError(exception))
            {
                Debug.LogWarning($"[GameBootstrap] 캐시 데이터 검증 실패. 전체 CSV를 다시 다운로드합니다: {exception.Message}");
                ResetDatabases();
                CsvLoader.EndInitialization();
                CsvLoader.BeginInitialization(cacheStore, false);
                prepared = await PrepareDefinitions();
            }

            _currentStage = BootstrapStage.GameSession;
            _session.Initialize(_runeEffectDatabase,
                _playerRuneManager,
                prepared.runeBook,
                prepared.definitionSet.Researches,
                prepared.definitionSet);

            CsvLoader.SaveInitializationCache();
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
        finally
        {
            if (ownsCsvInitialization)
                CsvLoader.EndInitialization();
        }
    }

    private static bool IsCacheDataError(Exception exception) =>
        exception is InvalidDataException or InvalidOperationException or FormatException or CsvHelper.CsvHelperException;

    private async UniTask<(GameDefinitionSet definitionSet, RuneBook runeBook)> PrepareDefinitions()
    {
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
        return (definitionSet, runeBook);
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