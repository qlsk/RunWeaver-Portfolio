using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// [테스트 전용] 룬북 UI를 게임 시스템 없이 확인하기 위한 가짜 데이터 소스.
/// 룬 효과 22종과 룬석 21종은 Rune Weaver Database를 기준으로 하며, 장착 규칙은 RuneLoadout을 따른다.
/// 룬 워드는 DB 값 대신 영문명을 룬 문자로 옮겨 만든다.
///
/// 장착 규칙 (기획 확정안, 팀원 RuneLoadout 반영 전까지 테스트용으로 먼저 적용)
/// - 1번 칸: 메인 원소 전용. 시작 시 무료로 장착되어 있으며 교체만 가능하고 비울 수 없다.
/// - 2~5번 칸: 원소/비원소 모두 가능. 단 서브 원소는 2~5번 칸 전체에서 1개만.
/// - 상호 배타: Return, Fix, Orbit은 서로 함께 장착할 수 없다. (룬 상호작용 문서 23장)
///
/// 테스트 키
/// 1: 모든 룬석 추가
/// 2: 다음 카테고리 공개 (Behaviour → Control → Special)
/// 3: 활성 칸 1칸 확장 (최대 5칸)
/// 키 입력이 안 될 때는 인스펙터에서 컴포넌트 우클릭 메뉴로도 실행할 수 있다.
/// </summary>
public class FakeRuneBookDataSource : RuneBookDataSource
{
    private const int SlotLimit = 5;
    private const int MinActiveSlotCount = 2;
    private const int MainElementSlotIndex = 0;

    [Header("시작 상태")]
    [SerializeField, Range(MinActiveSlotCount, SlotLimit)] private int _startActiveSlotCount = MinActiveSlotCount;
    [SerializeField, Min(0)] private int _startRuneStoneQuantity = 3;
    [Tooltip("게임 시작 시 무료로 1번 칸에 장착되는 메인 원소 (rune_fire, rune_water, rune_earth, rune_wind)")]
    [SerializeField] private string _startMainElementId = "rune_fire";

    [Header("아이콘 (비워두면 프리팹의 기본 이미지 사용)")]
    [SerializeField] private RuneStoneIconCatalog _runeStoneIconCatalog;
    [SerializeField] private RuneEffectIconCatalog _runeEffectIconCatalog;
    [Tooltip("카탈로그에 없는 룬 효과에 쓸 기본 아이콘")]
    [SerializeField] private Sprite _runeEffectIcon;

    [Header("테스트 키")]
    [SerializeField] private KeyCode _addRuneStonesKey = KeyCode.Alpha1;
    [SerializeField, Min(1)] private int _addRuneStoneQuantity = 1;
    [SerializeField] private KeyCode _revealNextCategoryKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode _expandSlotKey = KeyCode.Alpha3;

    private static readonly RuneEffectCategory[] s_categoryRevealOrder =
    {
        RuneEffectCategory.Behaviour,
        RuneEffectCategory.Control,
        RuneEffectCategory.Special
    };

    private static readonly (string Id, string EnglishName, RuneEffectCategory Category, bool CanOverlap, string Description)[]
        s_effectTable =
        {
            ("rune_fire", "Fire", RuneEffectCategory.Element, false, "Adds the Fire element to bullets."),
            ("rune_water", "Water", RuneEffectCategory.Element, false, "Adds the Water element to bullets."),
            ("rune_earth", "Earth", RuneEffectCategory.Element, false, "Adds the Earth element to bullets."),
            ("rune_wind", "Wind", RuneEffectCategory.Element, false, "Adds the Wind element to bullets."),
            ("rune_power", "Power", RuneEffectCategory.Enhancement, true,
                "Increases the power of bullets and elemental reactions by 50%."),
            ("rune_speed", "Speed", RuneEffectCategory.Enhancement, true, "Increases bullet speed by 50%."),
            ("rune_range", "Range", RuneEffectCategory.Enhancement, true, "Increases bullet range by 50%."),
            ("rune_grow", "Grow", RuneEffectCategory.Enhancement, true,
                "Bullet power gradually increases while traveling, up to 200%."),
            ("rune_duration", "Duration", RuneEffectCategory.Enhancement, true,
                "Increases the duration of elemental reaction effects."),
            ("rune_return", "Return", RuneEffectCategory.Behaviour, false,
                "Bullets return to the player after reaching maximum range."),
            ("rune_seek", "Seek", RuneEffectCategory.Behaviour, false,
                "Bullets automatically track enemies within a certain range."),
            ("rune_pierce", "Pierce", RuneEffectCategory.Behaviour, true,
                "Bullets pierce enemies instead of disappearing. Damage drops 5% per pierce (up to 30%)."),
            ("rune_chain", "Chain", RuneEffectCategory.Behaviour, true,
                "On hit, the elemental reaction chains once to nearby enemies at 50% damage."),
            ("rune_fix", "Fix", RuneEffectCategory.Behaviour, false,
                "At maximum range, bullets stay in place for 10 seconds before disappearing."),
            ("rune_orbit", "Orbit", RuneEffectCategory.Behaviour, false,
                "At maximum range, bullets orbit the player for 5 seconds."),
            ("rune_spread", "Spread", RuneEffectCategory.Behaviour, true,
                "Fires 3 bullets in a fan. Each bullet deals 50% less damage."),
            ("rune_multiple", "Multiple", RuneEffectCategory.Behaviour, true,
                "Fires bullets twice in a row. Each bullet deals 40% less damage."),
            ("rune_bind", "Bind", RuneEffectCategory.Control, true,
                "Binds enemies hit for 1 second, restricting their movement."),
            ("rune_absorb", "Absorb", RuneEffectCategory.Control, true,
                "Restores health equal to 10% of the damage dealt on hit."),
            ("rune_break", "Break", RuneEffectCategory.Control, false,
                "On hit, destroys enemy shields or applies a weakening effect."),
            ("rune_reverse", "Reverse", RuneEffectCategory.Special, true,
                "Bullets survive their first hit and reverse direction once."),
            ("rune_echo", "Echo", RuneEffectCategory.Special, true,
                "After an elemental reaction occurs, the same reaction triggers once more after a delay.")
        };

    /// <summary>
    /// 서로 함께 장착할 수 없는 룬 묶음. 같은 묶음 안의 서로 다른 룬은 동시에 장착할 수 없다.
    /// </summary>
    private static readonly string[][] s_exclusiveGroups =
    {
        new[] { "rune_return", "rune_fix", "rune_orbit" }
    };

    private static readonly (string Id, char RuneCharacter)[] s_runeStoneTable =
    {
        ("fehu", 'ᚠ'), ("uruz", 'ᚢ'), ("thurisaz", 'ᚦ'), ("ansuz", 'ᚨ'),
        ("raidho", 'ᚱ'), ("kenaz", 'ᚲ'), ("gebo", 'ᚷ'), ("wunjo", 'ᚹ'),
        ("hagalaz", 'ᚺ'), ("nauthiz", 'ᚾ'), ("isa", 'ᛁ'), ("jera", 'ᛃ'),
        ("algiz", 'ᛉ'), ("sowilo", 'ᛊ'), ("tiwaz", 'ᛏ'), ("berkano", 'ᛒ'),
        ("ehwaz", 'ᛖ'), ("mannaz", 'ᛗ'), ("laguz", 'ᛚ'), ("othala", 'ᛟ'),
        ("dagaz", 'ᛞ')
    };

    private sealed class FakeEntry
    {
        public string Id;
        public string RuneWord;
        public string EnglishName;
        public string Description;
        public RuneEffectCategory Category;
        public bool CanOverlap;
        public RuneWordRevealLevel RevealLevel;
    }

    private readonly List<FakeEntry> _entries = new();
    private readonly Dictionary<char, int> _runeStoneQuantities = new();
    private readonly Dictionary<char, Sprite> _runeStoneIcons = new();
    private readonly string[] _equippedEntryIds = new string[SlotLimit];
    private int _activeSlotCount;
    private int _nextCategoryIndex;
    private bool _isInitialized;

    public override int MaxSlotCount => SlotLimit;

    public override int ActiveSlotCount
    {
        get
        {
            EnsureInitialized();
            return _activeSlotCount;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// 룬북 UI가 이 컴포넌트의 Awake보다 먼저 데이터를 요청할 수 있으므로, 처음 요청될 때 데이터를 준비한다.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _activeSlotCount = Mathf.Clamp(_startActiveSlotCount, MinActiveSlotCount, SlotLimit);

        foreach ((string id, char runeCharacter) in s_runeStoneTable)
        {
            _runeStoneQuantities[runeCharacter] = _startRuneStoneQuantity;
            _runeStoneIcons[runeCharacter] = FindRuneStoneIcon(id);
        }

        foreach ((string id, string englishName, RuneEffectCategory category, bool canOverlap, string description) in
                 s_effectTable)
        {
            _entries.Add(new FakeEntry
            {
                Id = id,
                RuneWord = ToRuneWord(englishName),
                EnglishName = englishName,
                Description = description,
                Category = category,
                CanOverlap = canOverlap,
                // 모든 룬 워드는 처음부터 목록에 공개된다. (Hidden 폐기)
                RevealLevel = RuneWordRevealLevel.Listed
            });
        }

        EquipStartMainElement();

        Debug.Log($"[FakeRuneBookDataSource] 테스트 데이터 준비 완료. " +
                  $"{_addRuneStonesKey}: 룬석 추가, {_revealNextCategoryKey}: 카테고리 공개, {_expandSlotKey}: 활성 칸 확장");
    }

    private void Update()
    {
        if (Input.GetKeyDown(_addRuneStonesKey))
        {
            AddAllRuneStones(_addRuneStoneQuantity);
        }

        if (Input.GetKeyDown(_revealNextCategoryKey))
        {
            RevealNextCategory();
        }

        if (Input.GetKeyDown(_expandSlotKey))
        {
            ExpandActiveSlot();
        }
    }

    public override IReadOnlyList<RuneBookEntryData> GetEntries()
    {
        EnsureInitialized();
        List<RuneBookEntryData> entries = new(_entries.Count);
        foreach (FakeEntry entry in _entries)
        {
            entries.Add(new RuneBookEntryData(entry.Id, entry.RuneWord, entry.EnglishName, entry.Description,
                entry.Category, entry.RevealLevel, FindRuneEffectIcon(entry.Id)));
        }

        return entries;
    }

    public override IReadOnlyList<RuneStoneStackData> GetOwnedRuneStones()
    {
        EnsureInitialized();
        List<RuneStoneStackData> runeStones = new();
        foreach ((string id, char runeCharacter) in s_runeStoneTable)
        {
            int quantity = _runeStoneQuantities[runeCharacter];
            if (quantity > 0)
            {
                runeStones.Add(new RuneStoneStackData(id, runeCharacter, _runeStoneIcons[runeCharacter], quantity));
            }
        }

        return runeStones;
    }

    public override string GetEquippedEntryId(int slotIndex)
    {
        EnsureInitialized();
        return IsActiveSlot(slotIndex) ? _equippedEntryIds[slotIndex] : null;
    }

    public override bool CanEquip(int slotIndex, string entryId)
    {
        EnsureInitialized();
        if (!IsActiveSlot(slotIndex))
        {
            return false;
        }

        FakeEntry entry = FindEntry(entryId);
        if (entry == null)
        {
            return false;
        }

        if (_equippedEntryIds[slotIndex] == entryId)
        {
            return false;
        }

        if (!entry.CanOverlap && Array.Exists(_equippedEntryIds, id => id == entryId))
        {
            return false;
        }

        bool isElementEntry = entry.Category == RuneEffectCategory.Element;

        // 1번 칸은 메인 원소 전용
        if (slotIndex == MainElementSlotIndex && !isElementEntry)
        {
            return false;
        }

        // 서브 원소는 2~5번 칸 전체에서 1개만 (같은 칸의 교체는 허용)
        if (slotIndex != MainElementSlotIndex && isElementEntry && HasSubElementExcept(slotIndex))
        {
            return false;
        }

        // Return, Fix, Orbit 등 상호 배타 룬은 다른 칸에 있으면 장착 불가 (같은 칸의 교체는 허용)
        if (HasExclusiveConflict(slotIndex, entryId))
        {
            return false;
        }

        return HasEnoughRuneStones(entry.RuneWord);
    }

    public override bool TryEquip(int slotIndex, string entryId)
    {
        if (!CanEquip(slotIndex, entryId))
        {
            return false;
        }

        FakeEntry entry = FindEntry(entryId);
        ConsumeRuneStones(entry.RuneWord);
        _equippedEntryIds[slotIndex] = entryId;

        // 룬을 실제로 활성화하면 상세 정보가 공개된다.
        entry.RevealLevel = RuneWordRevealLevel.Detailed;

        Debug.Log($"[FakeRuneBookDataSource] {slotIndex + 1}번 칸에 {entry.EnglishName} 활성화 (룬석 {entry.RuneWord} 소모)");
        NotifyChanged();
        return true;
    }

    public override bool CanUnequip(int slotIndex)
    {
        EnsureInitialized();
        // 1번 칸(메인 원소)은 교체만 가능하고 비울 수 없다.
        return IsActiveSlot(slotIndex) && slotIndex != MainElementSlotIndex
                                       && !string.IsNullOrEmpty(_equippedEntryIds[slotIndex]);
    }

    public override bool TryUnequip(int slotIndex)
    {
        if (!CanUnequip(slotIndex))
        {
            return false;
        }

        // 기획서 3-2: 장착에 사용한 룬석은 환급되지 않는다.
        _equippedEntryIds[slotIndex] = null;
        Debug.Log($"[FakeRuneBookDataSource] {slotIndex + 1}번 칸 비활성화");
        NotifyChanged();
        return true;
    }

    [ContextMenu("Test/Add Rune Stones")]
    private void AddRuneStonesFromMenu()
    {
        if (!IsPlayingForMenu())
        {
            return;
        }

        AddAllRuneStones(_addRuneStoneQuantity);
    }

    private void AddAllRuneStones(int quantity)
    {
        foreach ((string _, char runeCharacter) in s_runeStoneTable)
        {
            _runeStoneQuantities[runeCharacter] += quantity;
        }

        Debug.Log($"[FakeRuneBookDataSource] 모든 룬석 +{quantity}");
        NotifyChanged();
    }

    [ContextMenu("Test/Reveal Next Category")]
    private void RevealNextCategory()
    {
        if (!IsPlayingForMenu())
        {
            return;
        }

        if (_nextCategoryIndex >= s_categoryRevealOrder.Length)
        {
            Debug.Log("[FakeRuneBookDataSource] 모든 카테고리가 이미 공개되었습니다.");
            return;
        }

        RuneEffectCategory category = s_categoryRevealOrder[_nextCategoryIndex];
        _nextCategoryIndex++;

        // 모든 룬 워드가 처음부터 공개되므로 바꿀 것이 없다. (Hidden 폐기) 테스트 키 호환용으로 남겨 둔다.
        Debug.Log($"[FakeRuneBookDataSource] {category} 카테고리 공개");
        NotifyChanged();
    }

    [ContextMenu("Test/Expand Active Slot")]
    private void ExpandActiveSlot()
    {
        if (!IsPlayingForMenu())
        {
            return;
        }

        if (_activeSlotCount >= SlotLimit)
        {
            Debug.Log($"[FakeRuneBookDataSource] 활성 칸이 이미 최대({SlotLimit}칸)입니다.");
            return;
        }

        _activeSlotCount++;
        Debug.Log($"[FakeRuneBookDataSource] 활성 칸 확장: {_activeSlotCount}칸");
        NotifyChanged();
    }

    private static bool IsPlayingForMenu()
    {
        if (Application.isPlaying)
        {
            return true;
        }

        Debug.LogWarning("[FakeRuneBookDataSource] 테스트 메뉴는 플레이 모드에서만 사용할 수 있습니다.");
        return false;
    }

    private void EquipStartMainElement()
    {
        FakeEntry mainElement = FindEntry(_startMainElementId);
        if (mainElement == null || mainElement.Category != RuneEffectCategory.Element)
        {
            Debug.LogWarning($"[FakeRuneBookDataSource] 시작 메인 원소 '{_startMainElementId}'를 찾지 못해 첫 원소 룬을 사용합니다.", this);
            mainElement = _entries.Find(entry => entry.Category == RuneEffectCategory.Element);
        }

        if (mainElement == null)
        {
            return;
        }

        // 시작 메인 원소는 무료로 지급되므로 룬석을 소모하지 않는다.
        _equippedEntryIds[MainElementSlotIndex] = mainElement.Id;
        mainElement.RevealLevel = RuneWordRevealLevel.Detailed;
    }

    private bool HasSubElementExcept(int slotIndex)
    {
        for (int i = 0; i < _activeSlotCount; i++)
        {
            if (i == MainElementSlotIndex || i == slotIndex)
            {
                continue;
            }

            FakeEntry equipped = FindEntry(_equippedEntryIds[i]);
            if (equipped != null && equipped.Category == RuneEffectCategory.Element)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasExclusiveConflict(int slotIndex, string entryId)
    {
        for (int i = 0; i < _activeSlotCount; i++)
        {
            string equippedId = _equippedEntryIds[i];
            if (i == slotIndex || string.IsNullOrEmpty(equippedId) || equippedId == entryId)
            {
                continue;
            }

            if (AreMutuallyExclusive(entryId, equippedId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AreMutuallyExclusive(string entryIdA, string entryIdB)
    {
        foreach (string[] group in s_exclusiveGroups)
        {
            if (Array.IndexOf(group, entryIdA) >= 0 && Array.IndexOf(group, entryIdB) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsActiveSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _activeSlotCount;
    }

    private FakeEntry FindEntry(string entryId)
    {
        return _entries.Find(entry => entry.Id == entryId);
    }

    private bool HasEnoughRuneStones(string runeWord)
    {
        Dictionary<char, int> requirements = new();
        foreach (char runeCharacter in runeWord)
        {
            requirements.TryGetValue(runeCharacter, out int required);
            requirements[runeCharacter] = required + 1;
        }

        foreach ((char runeCharacter, int required) in requirements)
        {
            if (!_runeStoneQuantities.TryGetValue(runeCharacter, out int owned) || owned < required)
            {
                return false;
            }
        }

        return true;
    }

    private void ConsumeRuneStones(string runeWord)
    {
        foreach (char runeCharacter in runeWord)
        {
            _runeStoneQuantities[runeCharacter]--;
        }
    }

    private Sprite FindRuneEffectIcon(string id)
    {
        if (_runeEffectIconCatalog == null)
        {
            return _runeEffectIcon;
        }

        try
        {
            Sprite icon = _runeEffectIconCatalog.GetIcon(id).Icon;
            return icon != null ? icon : _runeEffectIcon;
        }
        catch (ArgumentException)
        {
            Debug.LogWarning($"[FakeRuneBookDataSource] 룬 효과 아이콘을 찾지 못했습니다: {id}");
            return _runeEffectIcon;
        }
    }

    private Sprite FindRuneStoneIcon(string id)
    {
        if (_runeStoneIconCatalog == null)
        {
            return null;
        }

        try
        {
            return _runeStoneIconCatalog.GetIcon(id).Icon;
        }
        catch (ArgumentException)
        {
            Debug.LogWarning($"[FakeRuneBookDataSource] 룬석 아이콘을 찾지 못했습니다: {id}");
            return null;
        }
    }

    /// <summary>
    /// 영문명을 룬 문자열로 옮긴다. 실제 룬 워드는 DB의 RuneWord 값을 사용해야 한다.
    /// </summary>
    private static string ToRuneWord(string englishName)
    {
        string lower = englishName.ToLowerInvariant();
        StringBuilder builder = new(lower.Length);

        for (int i = 0; i < lower.Length; i++)
        {
            if (lower[i] == 't' && i + 1 < lower.Length && lower[i + 1] == 'h')
            {
                builder.Append('ᚦ'); // thurisaz
                i++;
                continue;
            }

            builder.Append(ToRuneCharacter(lower[i]));
        }

        return builder.ToString();
    }

    private static char ToRuneCharacter(char letter)
    {
        return letter switch
        {
            'a' => 'ᚨ', // ansuz
            'b' or 'p' => 'ᛒ', // berkano (perthro는 미사용 룬)
            'c' or 'k' or 'q' or 'x' => 'ᚲ', // kenaz
            'd' => 'ᛞ', // dagaz
            'e' => 'ᛖ', // ehwaz
            'f' => 'ᚠ', // fehu
            'g' => 'ᚷ', // gebo
            'h' => 'ᚺ', // hagalaz
            'i' => 'ᛁ', // isa
            'j' or 'y' => 'ᛃ', // jera
            'l' => 'ᛚ', // laguz
            'm' => 'ᛗ', // mannaz
            'n' => 'ᚾ', // nauthiz
            'o' => 'ᛟ', // othala
            'r' => 'ᚱ', // raidho
            's' => 'ᛊ', // sowilo
            't' => 'ᛏ', // tiwaz
            'u' => 'ᚢ', // uruz
            'v' or 'w' => 'ᚹ', // wunjo
            'z' => 'ᛉ', // algiz
            _ => 'ᛁ'
        };
    }
}
