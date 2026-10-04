using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// [테스트 전용] 새 룬북 화면을 게임 시스템 없이 확인하기 위한 가짜 데이터.
/// 룬 효과 22종과 룬 문자 21종은 DB 기준이고, 룬 워드는 영문명을 룬 문자로 옮겨 만든다.
///
/// 테스트 키
/// 1: 모든 룬 문자 1개씩 추가
/// 2: 활성 칸 1칸 확장 (최대 5칸)
/// 3: 숨겨진 카테고리 공개 (Control → Special)
/// </summary>
public class FakeRuneBookV2DataSource : RuneBookV2DataSource
{
    [Header("시작 상태")]
    [SerializeField, Range(2, SlotLimit)] private int _startActiveSlotCount = 3;
    [SerializeField] private string _startMainElementId = "rune_fire";
    [SerializeField] private string[] _startDetailedIds = { "rune_power", "rune_pierce", "rune_spread" };
    [Tooltip("룬 문자별 시작 개수 (순서는 ᚠ ᚢ ᚦ ᚨ ᚱ ᚲ ᚷ ᚹ ᚺ ᚾ ᛁ ᛃ ᛉ ᛊ ᛏ ᛒ ᛖ ᛗ ᛚ ᛟ ᛞ)")]
    [SerializeField] private int[] _startRuneStones = { 1, 1, 2, 3, 3, 0, 4, 2, 0, 1, 2, 0, 2, 2, 2, 2, 2, 0, 1, 2, 0 };

    [Header("아이콘")]
    [SerializeField] private RuneStoneIconCatalog _runeStoneIconCatalog;
    [SerializeField] private RuneEffectIconCatalog _runeEffectIconCatalog;

    [Header("테스트 키")]
    [SerializeField] private KeyCode _addRuneStonesKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _expandSlotKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode _revealCategoryKey = KeyCode.Alpha3;

    private static readonly (string Id, string Name, RuneEffectCategory Category, bool CanOverlap, string Description)[] s_effects =
    {
        ("rune_fire", "Fire", RuneEffectCategory.Element, false, "Adds the Fire element to bullets."),
        ("rune_water", "Water", RuneEffectCategory.Element, false, "Adds the Water element to bullets."),
        ("rune_earth", "Earth", RuneEffectCategory.Element, false, "Adds the Earth element to bullets."),
        ("rune_wind", "Wind", RuneEffectCategory.Element, false, "Adds the Wind element to bullets."),
        ("rune_power", "Power", RuneEffectCategory.Enhancement, true, "Increases the power of bullets and elemental reactions by 50%."),
        ("rune_speed", "Speed", RuneEffectCategory.Enhancement, true, "Increases bullet speed by 50%."),
        ("rune_range", "Range", RuneEffectCategory.Enhancement, true, "Increases bullet range by 50%."),
        ("rune_grow", "Grow", RuneEffectCategory.Enhancement, true, "Bullet power gradually increases while traveling, up to 200%."),
        ("rune_duration", "Duration", RuneEffectCategory.Enhancement, true, "Increases the duration of elemental reaction effects."),
        ("rune_return", "Return", RuneEffectCategory.Behaviour, false, "Bullets return to the player after reaching maximum range."),
        ("rune_seek", "Seek", RuneEffectCategory.Behaviour, false, "Bullets automatically track enemies within a certain range."),
        ("rune_pierce", "Pierce", RuneEffectCategory.Behaviour, true, "Bullets pierce enemies instead of disappearing. Damage drops 5% per pierce (up to 30%)."),
        ("rune_chain", "Chain", RuneEffectCategory.Behaviour, true, "On hit, the elemental reaction chains once to nearby enemies at 50% damage."),
        ("rune_fix", "Fix", RuneEffectCategory.Behaviour, false, "At maximum range, bullets stay in place for 10 seconds before disappearing."),
        ("rune_orbit", "Orbit", RuneEffectCategory.Behaviour, false, "At maximum range, bullets orbit the player for 5 seconds."),
        ("rune_spread", "Spread", RuneEffectCategory.Behaviour, true, "Fires 3 bullets in a fan. Each bullet deals 50% less damage."),
        ("rune_multiple", "Multiple", RuneEffectCategory.Behaviour, true, "Fires bullets twice in a row. Each bullet deals 40% less damage."),
        ("rune_bind", "Bind", RuneEffectCategory.Control, true, "Binds enemies hit for 1 second, restricting their movement."),
        ("rune_absorb", "Absorb", RuneEffectCategory.Control, true, "Restores health equal to 10% of the damage dealt on hit."),
        ("rune_break", "Break", RuneEffectCategory.Control, false, "On hit, destroys enemy shields or applies a weakening effect."),
        ("rune_reverse", "Reverse", RuneEffectCategory.Special, true, "Bullets survive their first hit and reverse direction once."),
        ("rune_echo", "Echo", RuneEffectCategory.Special, true, "After an elemental reaction occurs, the same reaction triggers once more after a delay.")
    };

    private static readonly (string Id, char Rune)[] s_runeStones =
    {
        ("fehu", 'ᚠ'), ("uruz", 'ᚢ'), ("thurisaz", 'ᚦ'), ("ansuz", 'ᚨ'), ("raidho", 'ᚱ'), ("kenaz", 'ᚲ'),
        ("gebo", 'ᚷ'), ("wunjo", 'ᚹ'), ("hagalaz", 'ᚺ'), ("nauthiz", 'ᚾ'), ("isa", 'ᛁ'), ("jera", 'ᛃ'),
        ("algiz", 'ᛉ'), ("sowilo", 'ᛊ'), ("tiwaz", 'ᛏ'), ("berkano", 'ᛒ'), ("ehwaz", 'ᛖ'), ("mannaz", 'ᛗ'),
        ("laguz", 'ᛚ'), ("othala", 'ᛟ'), ("dagaz", 'ᛞ')
    };

    private static readonly RuneEffectCategory[] s_revealOrder = { RuneEffectCategory.Control, RuneEffectCategory.Special };

    private readonly Dictionary<string, RuneWordRevealLevel> _reveal = new();
    private readonly Dictionary<char, int> _quantities = new();
    private readonly Dictionary<char, Sprite> _stoneIcons = new();
    private readonly string[] _confirmed = new string[SlotLimit];
    private int _activeSlotCount;
    private int _revealStep;
    private bool _isInitialized;

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

    private void Update()
    {
        if (Input.GetKeyDown(_addRuneStonesKey))
        {
            foreach ((string _, char rune) in s_runeStones)
            {
                _quantities[rune]++;
            }

            NotifyChanged();
        }

        if (Input.GetKeyDown(_expandSlotKey) && _activeSlotCount < SlotLimit)
        {
            _activeSlotCount++;
            NotifyChanged();
        }

        if (Input.GetKeyDown(_revealCategoryKey) && _revealStep < s_revealOrder.Length)
        {
            RuneEffectCategory category = s_revealOrder[_revealStep++];
            foreach ((string id, string _, RuneEffectCategory c, bool _, string _) in s_effects)
            {
                if (c == category)
                {
                    _reveal[id] = RuneWordRevealLevel.Listed;
                }
            }

            NotifyChanged();
        }
    }

    public override IReadOnlyList<RuneBookEntryData> GetEntries()
    {
        EnsureInitialized();
        List<RuneBookEntryData> entries = new(s_effects.Length);
        foreach ((string id, string name, RuneEffectCategory category, bool _, string description) in s_effects)
        {
            entries.Add(new RuneBookEntryData(id, ToRuneWord(name), name, description, category, _reveal[id],
                FindEffectIcon(id)));
        }

        return entries;
    }

    public override bool CanOverlap(string entryId)
    {
        foreach ((string id, string _, RuneEffectCategory _, bool canOverlap, string _) in s_effects)
        {
            if (id == entryId)
            {
                return canOverlap;
            }
        }

        return false;
    }

    public override IReadOnlyList<RuneStoneStackData> GetRuneStones()
    {
        EnsureInitialized();
        List<RuneStoneStackData> stones = new(s_runeStones.Length);
        foreach ((string id, char rune) in s_runeStones)
        {
            stones.Add(new RuneStoneStackData(id, rune, _stoneIcons[rune], _quantities[rune]));
        }

        return stones;
    }

    public override bool HasEnoughRuneStones(IReadOnlyDictionary<char, int> requirements)
    {
        EnsureInitialized();
        foreach ((char rune, int required) in requirements)
        {
            int owned = _quantities.TryGetValue(rune, out int quantity) ? quantity : 0;
            if (owned < required)
                return false;
        }
        return true;
    }

    public override string GetConfirmedId(int slotIndex)
    {
        EnsureInitialized();
        return slotIndex >= 0 && slotIndex < SlotLimit ? _confirmed[slotIndex] : null;
    }

    public override bool Confirm(IReadOnlyList<string> slotIds, IReadOnlyList<bool> newlyAdded)
    {
        EnsureInitialized();
        for (int i = 0; i < SlotLimit; i++)
        {
            string id = i < slotIds.Count ? slotIds[i] : null;
            _confirmed[i] = id;
            if (id == null || i >= newlyAdded.Count || !newlyAdded[i])
            {
                continue;
            }

            foreach (char rune in GetRuneWord(id))
            {
                if (_quantities.ContainsKey(rune))
                {
                    _quantities[rune]--;
                }
            }

            _reveal[id] = RuneWordRevealLevel.Detailed;
        }

        Debug.Log("[FakeRuneBookV2DataSource] Confirmed: " + string.Join(", ", _confirmed), this);
        NotifyChanged();
        return true;
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _activeSlotCount = Mathf.Clamp(_startActiveSlotCount, 2, SlotLimit);

        for (int i = 0; i < s_runeStones.Length; i++)
        {
            char rune = s_runeStones[i].Rune;
            _quantities[rune] = _startRuneStones != null && i < _startRuneStones.Length ? _startRuneStones[i] : 0;
            _stoneIcons[rune] = FindStoneIcon(s_runeStones[i].Id);
        }

        foreach ((string id, string _, RuneEffectCategory category, bool _, string _) in s_effects)
        {
            _reveal[id] = category switch
            {
                RuneEffectCategory.Element or RuneEffectCategory.Enhancement => RuneWordRevealLevel.Detailed,
                RuneEffectCategory.Behaviour => RuneWordRevealLevel.Listed,
                _ => RuneWordRevealLevel.Hidden
            };
        }

        if (_startDetailedIds != null)
        {
            foreach (string id in _startDetailedIds)
            {
                if (!string.IsNullOrEmpty(id) && _reveal.ContainsKey(id))
                {
                    _reveal[id] = RuneWordRevealLevel.Detailed;
                }
            }
        }

        _confirmed[0] = _startMainElementId;
    }

    private static string GetRuneWord(string id)
    {
        foreach ((string effectId, string name, RuneEffectCategory _, bool _, string _) in s_effects)
        {
            if (effectId == id)
            {
                return ToRuneWord(name);
            }
        }

        return string.Empty;
    }

    private Sprite FindStoneIcon(string id)
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
            return null;
        }
    }

    private Sprite FindEffectIcon(string id)
    {
        if (_runeEffectIconCatalog == null)
        {
            return null;
        }

        try
        {
            return _runeEffectIconCatalog.GetIcon(id).Icon;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>영문명을 룬 문자열로 옮긴다. (실제 룬 워드는 DB의 RuneWord 값을 쓴다)</summary>
    private static string ToRuneWord(string englishName)
    {
        string lower = englishName.ToLowerInvariant();
        StringBuilder builder = new(lower.Length);
        for (int i = 0; i < lower.Length; i++)
        {
            if (lower[i] == 't' && i + 1 < lower.Length && lower[i + 1] == 'h')
            {
                builder.Append('ᚦ');
                i++;
                continue;
            }

            builder.Append(lower[i] switch
            {
                'a' => 'ᚨ',
                'b' or 'p' => 'ᛒ',
                'c' or 'k' or 'q' or 'x' => 'ᚲ',
                'd' => 'ᛞ',
                'e' => 'ᛖ',
                'f' => 'ᚠ',
                'g' => 'ᚷ',
                'h' => 'ᚺ',
                'i' => 'ᛁ',
                'j' or 'y' => 'ᛃ',
                'l' => 'ᛚ',
                'm' => 'ᛗ',
                'n' => 'ᚾ',
                'o' => 'ᛟ',
                'r' => 'ᚱ',
                's' => 'ᛊ',
                't' => 'ᛏ',
                'u' => 'ᚢ',
                'v' or 'w' => 'ᚹ',
                'z' => 'ᛉ',
                _ => 'ᛁ'
            });
        }

        return builder.ToString();
    }
}
