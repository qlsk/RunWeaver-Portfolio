using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 새 룬북(RuneBookV2)을 실제 게임 데이터에 연결하는 데이터 소스.
/// - 룬 워드 목록 / 공개 단계 : GameSession의 RuneBook + RuneEffectDatabase (모든 룬 워드는 처음부터 Listed)
/// - 보유 룬 문자 : PlayerRuneManager의 RuneStoneInventory (RuneStoneDatabase 순서대로, 0개 포함)
/// - 활성 칸 수 / 확정된 장착 : PlayerRuneManager의 RuneEffectLoadout
/// - Confirm : 새로 넣은 칸의 룬 워드만큼 룬 문자를 소모하고, 장착한 룬 워드의 설명을 공개한다.
///   (장착 확정 자체는 RuneBookV2Presenter가 RuneEffectLoadout.ConfirmEquipment로 한다)
/// GameSession, PlayerRuneManager, DB가 준비될 때까지 기다렸다가 연결하고 Changed를 보낸다.
/// </summary>
public class GameRuneBookV2DataSource : RuneBookV2DataSource
{
    private const int DefaultActiveSlotCount = 2;

    [Header("아이콘 (DB에 아이콘이 없을 때 사용)")]
    [SerializeField] private RuneStoneIconCatalog _runeStoneIconCatalog;
    [SerializeField] private RuneEffectIconCatalog _runeEffectIconCatalog;

    [Header("테스트")]
    [Tooltip("누르면 모든 룬 문자를 종류별로 1개씩 얻는다. (에디터, 개발 빌드에서만 동작. 끄려면 None)")]
    [SerializeField] private KeyCode _testAddRuneStonesKey = KeyCode.F1;

    private readonly Dictionary<char, int> _wordRequirements = new();
    private RuneBook _runeBook;
    private PlayerRuneManager _runeManager;
    private RuneStoneInventory _inventory;
    private bool _isBound;
    private bool _hasLoggedWaiting;

    private RuneEffectLoadout Loadout => _runeManager != null ? _runeManager.RuneEffectLoadout : null;

    public override int ActiveSlotCount =>
        Loadout != null ? Mathf.Clamp(Loadout.ActiveSlotCount, 1, SlotLimit) : DefaultActiveSlotCount;

    protected override void OnEnable()
    {
        base.OnEnable();
        TryBind();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Unbind();
    }

    private void Update()
    {
        if (!_isBound)
        {
            TryBind();
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_testAddRuneStonesKey != KeyCode.None && Input.GetKeyDown(_testAddRuneStonesKey))
        {
            foreach (RuneStoneDefinition definition in RuneStoneDatabase.Instance.GetAll())
            {
                _inventory.TryAddRuneStone(definition.RuneCharacter, 1);
            }

            Debug.Log("[GameRuneBookV2DataSource] [테스트] 룬 문자를 종류별로 1개씩 지급했습니다.", this);
        }
#endif
    }

    // ───────────────────────── 연결 ─────────────────────────

    private void TryBind()
    {
        if (_isBound)
        {
            return;
        }

        GameSession session = GameSession.Instance;
        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        RuneEffectDatabase effectDatabase = RuneEffectDatabase.Instance;
        RuneStoneDatabase stoneDatabase = RuneStoneDatabase.Instance;
        bool isReady = session != null && session.IsReady && session.RuneBook != null
                       && runeManager != null && runeManager.IsInitialized
                       && effectDatabase != null && effectDatabase.DatabaseReady
                       && stoneDatabase != null && stoneDatabase.DatabaseReady;
        if (!isReady)
        {
            if (!_hasLoggedWaiting)
            {
                _hasLoggedWaiting = true;
                Debug.Log("[GameRuneBookV2DataSource] GameSession과 DB 준비를 기다리는 중입니다.", this);
            }

            return;
        }

        _runeBook = session.RuneBook;
        _runeManager = runeManager;
        _inventory = runeManager.RuneStoneInventory;
        _runeBook.RuneBookChanged += HandleSourceChanged;
        _runeManager.LoadoutReplaced += HandleLoadoutReplaced;
        _inventory.OnInventoryChanged += HandleSourceChanged;
        _isBound = true;
        NotifyChanged();
    }

    private void Unbind()
    {
        if (!_isBound)
        {
            return;
        }

        _runeBook.RuneBookChanged -= HandleSourceChanged;
        _runeManager.LoadoutReplaced -= HandleLoadoutReplaced;
        _inventory.OnInventoryChanged -= HandleSourceChanged;
        _runeBook = null;
        _runeManager = null;
        _inventory = null;
        _isBound = false;
    }

    private void HandleSourceChanged()
    {
        NotifyChanged();
    }

    private void HandleLoadoutReplaced()
    {
        // 새 게임 : 인벤토리는 같은 인스턴스를 비우므로 다시 연결할 필요가 없다.
        NotifyChanged();
    }

    // ───────────────────────── 조회 ─────────────────────────

    public override IReadOnlyList<RuneBookEntryData> GetEntries()
    {
        if (!_isBound)
        {
            return Array.Empty<RuneBookEntryData>();
        }

        IReadOnlyList<RuneEffectDefinition> effects = RuneEffectDatabase.Instance.GetAll();
        List<RuneBookEntryData> entries = new(effects.Count);
        foreach (RuneEffectDefinition effect in effects)
        {
            RuneWordRevealLevel reveal = _runeBook.GetWord(effect.Id).RevealLevel;

            // 완성품을 가지고 있으면 이미 아는 룬 워드로 본다.
            if (GetOwnedCount(effect.Id) > 0)
            {
                reveal = RuneWordRevealLevel.Detailed;
            }

            // DB 설명은 한글이라 룬북 폰트(영문)로 표시할 수 없으므로 영문 설명을 쓴다.
            string description = RuneEffectText.GetDescription(effect.Id, effect.Description);
            entries.Add(new RuneBookEntryData(effect.Id, effect.RuneWord, effect.EnglishName, description,
                effect.Category, reveal, FindEffectIcon(effect)));
        }

        return entries;
    }

    public override bool CanOverlap(string entryId)
    {
        if (Loadout != null)
        {
            return Loadout.CanOverlap(entryId);
        }

        RuneEffectDatabase effectDatabase = RuneEffectDatabase.Instance;
        return effectDatabase != null && effectDatabase.TryGetById(entryId, out RuneEffectDefinition effect) &&
               (effect.Category == RuneEffectCategory.Element || effect.CanOverlap);
    }

    public override IReadOnlyList<RuneStoneStackData> GetRuneStones()
    {
        if (!_isBound)
        {
            return Array.Empty<RuneStoneStackData>();
        }

        IReadOnlyList<RuneStoneDefinition> definitions = RuneStoneDatabase.Instance.GetAll();
        List<RuneStoneStackData> stones = new(definitions.Count);
        foreach (RuneStoneDefinition definition in definitions)
        {
            stones.Add(new RuneStoneStackData(definition.Id, definition.RuneCharacter, FindStoneIcon(definition),
                _inventory.GetQuantity(definition.RuneCharacter)));
        }

        return stones;
    }

    public override bool HasEnoughRuneStones(IReadOnlyDictionary<char, int> requirements)
    {
        return _isBound && _inventory.HasEnoughRuneStones(requirements);
    }

    public override string GetConfirmedId(int slotIndex)
    {
        return Loadout != null ? Loadout.GetEquippedEffectId(slotIndex) : null;
    }

    // ───────────────────────── 확정 ─────────────────────────

    public override bool Confirm(IReadOnlyList<string> slotIds, IReadOnlyList<bool> newlyAdded)
    {
        if (!_isBound || slotIds == null || newlyAdded == null)
        {
            return false;
        }

        // 일부만 소모되지 않도록 먼저 전체 필요량을 확인한다.
        _wordRequirements.Clear();
        for (int i = 0; i < slotIds.Count && i < newlyAdded.Count; i++)
        {
            if (newlyAdded[i] && !string.IsNullOrEmpty(slotIds[i]))
            {
                foreach (char rune in GetRuneWord(slotIds[i]))
                {
                    _wordRequirements.TryGetValue(rune, out int count);
                    _wordRequirements[rune] = count + 1;
                }
            }
        }

        if (!_inventory.HasEnoughRuneStones(_wordRequirements))
        {
            return false;
        }

        for (int i = 0; i < slotIds.Count && i < newlyAdded.Count; i++)
        {
            if (newlyAdded[i] && !string.IsNullOrEmpty(slotIds[i]))
            {
                _inventory.ConsumeRuneStones(GetRuneWord(slotIds[i]));
            }
        }

        // 장착한 룬 워드는 설명까지 공개한다.
        foreach (string id in slotIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                _runeBook.RevealDetails(id);
            }
        }

        NotifyChanged();
        return true;
    }

    // ───────────────────────── 도움 함수 ─────────────────────────

    private static string GetRuneWord(string effectId)
    {
        RuneEffectDatabase effectDatabase = RuneEffectDatabase.Instance;
        return effectDatabase != null && effectDatabase.TryGetById(effectId, out RuneEffectDefinition effect)
            ? effect.RuneWord ?? string.Empty
            : string.Empty;
    }

    private Sprite FindEffectIcon(RuneEffectDefinition effect)
    {
        if (effect.Icon != null || _runeEffectIconCatalog == null)
        {
            return effect.Icon;
        }

        try
        {
            return _runeEffectIconCatalog.GetIcon(effect.Id).Icon;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private Sprite FindStoneIcon(RuneStoneDefinition definition)
    {
        if (definition.Icon != null || _runeStoneIconCatalog == null)
        {
            return definition.Icon;
        }

        try
        {
            return _runeStoneIconCatalog.GetIcon(definition.Id).Icon;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
