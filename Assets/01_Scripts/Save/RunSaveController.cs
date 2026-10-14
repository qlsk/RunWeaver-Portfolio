using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 전투 씬에서 판 저장(층 시작 체크포인트)과 이어하기를 맡는다.
/// 씬에 따로 놓지 않는다. StageManager가 있는 씬이 열리면 스스로 만들어진다.
///
/// 저장 : 층에 들어와 미션이 정해졌을 때(StageStarted) 지금 상태를 저장한다.
///        층 사이 상점에서는 들어갈 때와 살 때, 리롤할 때마다 상점 진열까지 저장한다.
///        (리롤 결과가 마음에 안 들어 껐다 켜도 리롤 전으로 돌아가지 않는다)
///        튜토리얼을 끝내기 전에는 저장하지 않는다. (연습 판)
/// 삭제 : 판이 끝나면(RunEnded : 사망, 포기, 잠식, 클리어) 저장을 지운다. 실패 연출 중에 꺼서 되살리는 것을 막는다.
/// 이어하기 : 타이틀에서 이어하기로 들어왔으면 시작 석상을 치우고, 저장된 상태를 되돌린 뒤
///            저장된 층의 준비 단계부터 시작한다. 미션도 저장된 것과 같은 종류로 다시 준비한다.
///            상점에서 저장된 판이면 같은 진열로 상점을 다시 열고, 나가면 다음 층이 시작된다.
/// </summary>
[DisallowMultipleComponent]
public class RunSaveController : MonoBehaviour
{
    private const string ShieldTraitId = "shield_01";
    private const float ReadyTimeout = 10f;

    private StageManager _stageManager;
    private CatalogShopDataSource _shop;
    private bool _isRestoring;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindAnyObjectByType<RunSaveController>() != null || FindAnyObjectByType<StageManager>() == null)
        {
            return;
        }

        new GameObject(nameof(RunSaveController)).AddComponent<RunSaveController>();
    }

    private IEnumerator Start()
    {
        _stageManager = StageManager.Instance;
        _stageManager.StageStarted += HandleStageStarted;
        _stageManager.PhaseChanged += HandlePhaseChanged;
        _stageManager.RunEnded += HandleRunEnded;

        _shop = FindAnyObjectByType<CatalogShopDataSource>(FindObjectsInactive.Include);
        if (_shop != null)
        {
            _shop.Changed += HandleShopChanged;
        }

        if (!SaveSystem.ConsumeContinueRequest())
        {
            yield break;
        }

        // 씬의 다른 Start(StageManager, 보상 핸들러, 시작 석상 등)가 끝난 뒤에 되돌린다.
        _isRestoring = true;
        yield return null;

        float waited = 0f;
        while (!IsSessionReady() && waited < ReadyTimeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        RunSaveData run = SaveSystem.Run;
        if (run == null || !IsSessionReady())
        {
            Debug.LogError("[RunSaveController] 이어할 데이터를 불러오지 못해 처음부터 시작합니다.", this);
            _isRestoring = false;
            yield break;
        }

        RemovePreparationStatue();
        Restore(run);
        _isRestoring = false;

        if (run.InShop && _shop != null)
        {
            _shop.QueueRestore(run.ShopOffers, run.ShopRerollCount);
            _stageManager.BeginContinuedShop(run.Depth);
            Debug.Log($"[RunSaveController] {run.Depth}층 앞 상점부터 이어합니다.", this);
            yield break;
        }

        if (!string.IsNullOrEmpty(run.MissionType) &&
            Enum.TryParse(run.MissionType, out MissionType missionType) && MissionManager.Instance != null)
        {
            MissionManager.Instance.SetNextMissionOverride(missionType);
        }

        _stageManager.BeginContinuedRun(run.Depth);
        Debug.Log($"[RunSaveController] {run.Depth}층부터 이어합니다.", this);
    }

    private void OnDestroy()
    {
        if (_stageManager != null)
        {
            _stageManager.StageStarted -= HandleStageStarted;
            _stageManager.PhaseChanged -= HandlePhaseChanged;
            _stageManager.RunEnded -= HandleRunEnded;
        }

        if (_shop != null)
        {
            _shop.Changed -= HandleShopChanged;
        }
    }

    // ───────────────────────── 저장 / 삭제 ─────────────────────────

    private void HandleStageStarted(int depth)
    {
        TrySave();
    }

    /// <summary>상점에 들어갔을 때</summary>
    private void HandlePhaseChanged(StagePhase phase)
    {
        if (phase == StagePhase.Shop)
        {
            TrySave();
        }
    }

    /// <summary>상점에서 사거나 리롤했을 때 (진열, 골드, 리롤 비용이 바뀜)</summary>
    private void HandleShopChanged()
    {
        if (_stageManager != null && _stageManager.Phase == StagePhase.Shop)
        {
            TrySave();
        }
    }

    private void TrySave()
    {
        if (_isRestoring || RunFlow.HasEnded || !TutorialProgressStore.IsCompleted || !IsSessionReady())
        {
            return;
        }

        PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
        if (health != null && health.IsDead)
        {
            return;
        }

        SaveSystem.SaveRun(Capture(_stageManager.CurrentDepth, health));
    }

    private static void HandleRunEnded(RunEndReason reason)
    {
        SaveSystem.DeleteRun();
    }

    // ───────────────────────── 모으기 ─────────────────────────

    private RunSaveData Capture(int depth, PlayerHealth health)
    {
        var run = new RunSaveData { Depth = depth };

        // 상점 : 다음 층(depth)의 미션은 상점을 나간 뒤에 정해지므로 미션 종류는 비워 둔다.
        run.InShop = _stageManager.Phase == StagePhase.Shop;
        if (run.InShop && _shop != null)
        {
            run.ShopOffers = _shop.CaptureOffers();
            run.ShopRerollCount = _shop.RerollCount;
        }

        MissionManager missions = MissionManager.Instance;
        if (missions != null)
        {
            if (!run.InShop && !_stageManager.IsBossStage && missions.CurrentMissionType.HasValue)
            {
                run.MissionType = missions.CurrentMissionType.Value.ToString();
            }

            foreach (MissionType type in missions.GetMissionBag())
            {
                run.MissionBag.Add(type.ToString());
            }
        }

        run.Health = health != null ? health.CurrentHealth : 0f;

        Wallet wallet = GameSession.Instance.Current.Holdings.ExpeditionWallet;
        run.Gold = wallet != null ? wallet.Balance : 0;

        PlayerRuneManager runes = PlayerRuneManager.Instance;
        foreach (RuneStoneItem item in runes.RuneStoneInventory.GetAll())
        {
            if (item != null && item.RuneStoneDefinition != null && item.Quantity > 0)
            {
                run.RuneStones.Add(new RuneStoneSaveEntry
                {
                    Rune = item.RuneStoneDefinition.RuneCharacter.ToString(),
                    Count = item.Quantity,
                });
            }
        }

        run.EquippedEffectIds.AddRange(runes.RuneEffectLoadout.EquippedEffectIds);
        run.ActiveSlotCount = runes.RuneEffectLoadout.ActiveSlotCount;

        foreach (KeyValuePair<string, int> pair in OwnedRuneWords.GetAll())
        {
            run.OwnedRuneWords.Add(new CountSaveEntry { Id = pair.Key, Count = pair.Value });
        }

        run.TraitIds.AddRange(TraitManager.Instance.AcquiredIds);

        PassiveStatManager passives = PassiveStatManager.Instance;
        foreach (PassiveStatType type in passives.StackedTypes)
        {
            run.Passives.Add(new PassiveSaveEntry
            {
                Type = type.ToString(),
                Stacks = passives.GetStacks(type),
                ValuePerStack = passives.GetValuePerStack(type),
            });
        }

        GameRunResultSource result = FindAnyObjectByType<GameRunResultSource>();
        if (result != null)
        {
            run.ElapsedSeconds = result.ElapsedSeconds;
        }

        EnemyDeathTracker tracker = FindAnyObjectByType<EnemyDeathTracker>();
        if (tracker != null)
        {
            run.KillCount = tracker.KillCount;
            run.TotalDamage = tracker.TotalDamage;
        }

        return run;
    }

    // ───────────────────────── 되돌리기 ─────────────────────────

    private void Restore(RunSaveData run)
    {
        // 골드 : 시작 석상이 원정을 시작해 지갑을 만들어 둔다. 저장된 잔액에 맞춘다.
        RunFlow.StartExpedition();
        Wallet wallet = GameSession.Instance.Current.Holdings.ExpeditionWallet;
        if (wallet != null)
        {
            int difference = run.Gold - wallet.Balance;
            if (difference > 0)
            {
                wallet.AddGold(difference);
            }
            else if (difference < 0)
            {
                wallet.TrySpend(-difference);
            }
        }

        // 룬 문자
        PlayerRuneManager runes = PlayerRuneManager.Instance;
        runes.RuneStoneInventory.Clear();
        foreach (RuneStoneSaveEntry entry in run.RuneStones)
        {
            if (!string.IsNullOrEmpty(entry.Rune) && entry.Count > 0)
            {
                runes.RuneStoneInventory.TryAddRuneStone(entry.Rune[0], entry.Count);
            }
        }

        // 장착 칸 수와 장착 룬 (0번 칸이 주 원소)
        RuneEffectLoadout loadout = runes.RuneEffectLoadout;
        loadout.ResetSlots();
        while (loadout.ActiveSlotCount < run.ActiveSlotCount && loadout.ExpandSlots())
        {
        }

        try
        {
            loadout.ConfirmEquipment(run.EquippedEffectIds);
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogWarning($"[RunSaveController] 장착 룬을 되돌리지 못했습니다: {exception.Message}", this);
        }

        // 룬 워드 완성품
        OwnedRuneWords.Clear();
        foreach (CountSaveEntry entry in run.OwnedRuneWords)
        {
            OwnedRuneWords.Add(entry.Id, entry.Count);
        }

        RestoreTraits(run.TraitIds);

        PassiveStatManager passives = PassiveStatManager.Instance;
        passives.ResetAll();
        foreach (PassiveSaveEntry entry in run.Passives)
        {
            if (Enum.TryParse(entry.Type, out PassiveStatType type))
            {
                passives.RestoreStacks(type, entry.Stacks, entry.ValuePerStack);
            }
        }

        PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
        if (health != null)
        {
            health.RestoreHealth(run.Health);
        }

        GameRunResultSource result = FindAnyObjectByType<GameRunResultSource>();
        if (result != null)
        {
            result.RestoreElapsed(run.ElapsedSeconds);
        }

        EnemyDeathTracker tracker = FindAnyObjectByType<EnemyDeathTracker>();
        if (tracker != null)
        {
            tracker.Restore(run.KillCount, run.TotalDamage);
        }

        MissionManager missions = MissionManager.Instance;
        if (missions != null)
        {
            var bag = new List<MissionType>();
            foreach (string name in run.MissionBag)
            {
                if (Enum.TryParse(name, out MissionType type))
                {
                    bag.Add(type);
                }
            }

            missions.RestoreMissionBag(bag);
        }
    }

    private void RestoreTraits(List<string> traitIds)
    {
        TraitManager traits = TraitManager.Instance;
        traits.ResetAll();
        RewardGenerator generator = FindAnyObjectByType<RewardGenerator>();
        foreach (string id in traitIds)
        {
            TraitData data = generator != null ? generator.FindTrait(id) : null;
            if (data == null)
            {
                Debug.LogWarning($"[RunSaveController] 특성 데이터를 찾지 못해 건너뜁니다: {id}", this);
                continue;
            }

            // 보상 선택(RewardController.ApplyReward)과 같은 처리. shield_01은 획득할 때 한 번 최대치를 늘린다.
            if (traits.AcquireTrait(data) && id == ShieldTraitId)
            {
                Shield shield = FindAnyObjectByType<Shield>();
                if (shield != null)
                {
                    shield.ShieldMaxStackUp();
                }
            }
        }
    }

    /// <summary>이어하기에서는 출발 준비를 하지 않으므로 시작 석상과 안내를 치운다.</summary>
    private static void RemovePreparationStatue()
    {
        foreach (ExpeditionStatueHint hint in FindObjectsByType<ExpeditionStatueHint>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            hint.gameObject.SetActive(false);
        }

        ExpeditionStatue statue = FindAnyObjectByType<ExpeditionStatue>();
        if (statue != null)
        {
            statue.RemoveForContinuation();
        }
    }

    private static bool IsSessionReady()
    {
        GameSession session = GameSession.Instance;
        PlayerRuneManager runes = PlayerRuneManager.Instance;
        return session != null && session.IsReady && session.Current != null &&
               runes != null && runes.IsInitialized;
    }
}
