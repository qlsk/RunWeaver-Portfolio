using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// [테스트 전용] 룬북 UI를 게임 시스템 없이 확인하기 위한 가짜 데이터 소스.
/// 룬 효과 22종과 룬석 21종은 Rune Weaver Database를 기준으로 하며, 장착 규칙은 RuneLoadout을 따른다.
/// 룬 워드는 DB 값 대신 영문명을 룬 문자로 옮겨 만든다.
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
    private const int ElementSlotCount = 2;

    [Header("시작 상태")]
    [SerializeField, Range(MinActiveSlotCount, SlotLimit)] private int _startActiveSlotCount = MinActiveSlotCount;
    [SerializeField, Min(0)] private int _startRuneStoneQuantity = 3;

    [Header("아이콘 (비워두면 프리팹의 기본 이미지 사용)")]
    [SerializeField] private RuneStoneIconCatalog _runeStoneIconCatalog;
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

    public override int MaxSlotCount => SlotLimit;
    public override int ActiveSlotCount => _activeSlotCount;

    private void Awake()
    {
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
                // 기획서 14-2: Element와 Enhancement는 처음부터 목록에 공개된다.
                RevealLevel = category is RuneEffectCategory.Element or RuneEffectCategory.Enhancement
                    ? RuneWordRevealLevel.Listed
                    : RuneWordRevealLevel.Hidden
            });
        }

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
        List<RuneBookEntryData> entries = new(_entries.Count);
        foreach (FakeEntry entry in _entries)
        {
            entries.Add(new RuneBookEntryData(entry.Id, entry.RuneWord, entry.EnglishName, entry.Description,
                entry.Category, entry.RevealLevel, _runeEffectIcon));
        }

        return entries;
    }

    public override IReadOnlyList<RuneStoneStackData> GetOwnedRuneStones()
    {
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
        return IsActiveSlot(slotIndex) ? _equippedEntryIds[slotIndex] : null;
    }

    public override bool CanEquip(int slotIndex, string entryId)
    {
        if (!IsActiveSlot(slotIndex))
        {
            return false;
        }

        FakeEntry entry = FindEntry(entryId);
        if (entry == null || entry.RevealLevel == RuneWordRevealLevel.Hidden)
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

        // RuneLoadout 규칙: 1~2번째 칸은 원소 룬 전용, 나머지 칸은 원소 외 룬 전용
        bool isElementSlot = slotIndex < ElementSlotCount;
        bool isElementEntry = entry.Category == RuneEffectCategory.Element;
        if (isElementSlot != isElementEntry)
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
        return IsActiveSlot(slotIndex) && !string.IsNullOrEmpty(_equippedEntryIds[slotIndex]);
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
        if (_nextCategoryIndex >= s_categoryRevealOrder.Length)
        {
            Debug.Log("[FakeRuneBookDataSource] 모든 카테고리가 이미 공개되었습니다.");
            return;
        }

        RuneEffectCategory category = s_categoryRevealOrder[_nextCategoryIndex];
        _nextCategoryIndex++;

        foreach (FakeEntry entry in _entries)
        {
            if (entry.Category == category && entry.RevealLevel == RuneWordRevealLevel.Hidden)
            {
                entry.RevealLevel = RuneWordRevealLevel.Listed;
            }
        }

        Debug.Log($"[FakeRuneBookDataSource] {category} 카테고리 공개");
        NotifyChanged();
    }

    [ContextMenu("Test/Expand Active Slot")]
    private void ExpandActiveSlot()
    {
        if (_activeSlotCount >= SlotLimit)
        {
            Debug.Log($"[FakeRuneBookDataSource] 활성 칸이 이미 최대({SlotLimit}칸)입니다.");
            return;
        }

        _activeSlotCount++;
        Debug.Log($"[FakeRuneBookDataSource] 활성 칸 확장: {_activeSlotCount}칸");
        NotifyChanged();
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
