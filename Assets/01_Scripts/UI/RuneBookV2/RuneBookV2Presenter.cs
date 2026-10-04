using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 새 룬북(RuneBookV2)의 Rune Words 탭 + Rune Stones + Activated 띠를 묶는다.
///
/// 장착 규칙
/// - 장착 규칙은 RuneEffectLoadout에서 검사한다. Confirm 전까지 변경은 화면에만 보관한다.
/// - 원소를 넣으면 앞의 두 칸에 순서대로 삽입하고 기존 비원소 룬은 뒤로 민다.
/// - 1번 칸(MAIN)의 주 원소는 오른쪽 클릭으로 해제할 수 없다.
///   원소를 넣을 때 MAIN이 비어 있으면 MAIN에 넣는다.
/// - 서브 원소는 1개까지(MAIN과 같은 원소도 가능). 두 번째를 넣으면 "Only one sub element can be set".
/// - 원소가 아닌 룬은 마지막 장착 룬 뒤에 들어간다. 빈 칸이 없으면 "All slots are full".
/// - 겹치기가 안 되는 룬을 이미 넣었으면 "This rune cannot be stacked".
/// - 장착 칸을 오른쪽 클릭하면 뺀다. 이미 확정된 룬을 빼도 룬 문자는 돌려주지 않는다.
/// - 룬 문자가 모자라면(남는 수가 0 미만) Confirm을 막고 "Not enough rune stones"를 띄운다.
/// - Confirm 없이 룬북을 닫으면(이 오브젝트가 꺼지면) 바꾼 내용은 버린다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookV2Presenter : MonoBehaviour
{
    [SerializeField] private RuneBookV2DataSource _dataSource;

    [Header("Rune Words")]
    [SerializeField] private RuneWordTileView[] _tiles;
    [SerializeField] private RuneWordTooltipView _tooltip;

    [Header("Rune Stones (표시 순서대로)")]
    [SerializeField] private RuneStoneCellView[] _stoneCells;

    [Header("Activated")]
    [SerializeField] private RuneSocketView[] _sockets;
    [SerializeField] private TMP_Text _activatedText;
    [SerializeField] private string _activatedFormat = "Activated  {0} / {1}";

    [Header("확정 버튼")]
    [SerializeField] private Button _confirmButton;
    [SerializeField] private GameObject _confirmGlow;
    [SerializeField] private TMP_Text _confirmInfoText;
    [SerializeField] private Color _infoColor = new(0.941f, 0.745f, 0.431f, 1f);
    [SerializeField] private Color _warningColor = new(0.941f, 0.471f, 0.392f, 1f);

    [Header("안내")]
    [SerializeField] private RuneBookToastView _toast;

    [Header("문구")]
    [SerializeField] private string _usesFormat = "Uses {0} rune stones";
    [SerializeField] private string _usesOneText = "Uses 1 rune stone";
    [SerializeField] private string _notEnoughText = "Not enough rune stones";
    [SerializeField] private string _mainRequiredText = "Set a main element";
    [SerializeField] private string _subElementLimitText = "Only one sub element can be set";
    [SerializeField] private string _slotsFullText = "All slots are full";
    [SerializeField] private string _notStackableText = "This rune cannot be stacked";

    private readonly string[] _slotIds = new string[RuneBookV2DataSource.SlotLimit];
    private readonly bool[] _isNew = new bool[RuneBookV2DataSource.SlotLimit];
    private readonly Dictionary<string, RuneBookEntryData> _entries = new();
    private readonly Dictionary<char, RuneStoneStackData> _stones = new();
    private readonly Dictionary<char, int> _pendingUse = new();
    private readonly Dictionary<char, int> _hoverUse = new();
    private readonly List<RuneWordTooltipView.RuneStoneView> _tooltipStones = new();
    private readonly List<Sprite> _tooltipMissing = new();
    private IReadOnlyList<RuneStoneStackData> _stoneList = Array.Empty<RuneStoneStackData>();
    private RuneWordTileView _hoveredTile;
    private bool _isRefreshQueued;
    private bool _isInitialized;
    private RuneEffectLoadoutManager _loadoutManager;
    private RuneEffectLoadout _loadout;

    private int ActiveSlotCount => _loadout != null ? _loadout.ActiveSlotCount : 0;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        if (_dataSource == null)
        {
            return;
        }

        _dataSource.Changed += HandleDataChanged;
        BindLoadoutManager();
        _isRefreshQueued = true;
    }

    private void LateUpdate()
    {
        if (_loadoutManager != RuneEffectLoadoutManager.Instance)
            BindLoadoutManager();
        else if (_loadoutManager != null && _loadout != _loadoutManager.RuneEffectLoadout)
            BindLoadout();
        if (_isRefreshQueued)
        {
            _isRefreshQueued = false;
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (_dataSource != null)
        {
            _dataSource.Changed -= HandleDataChanged;
        }
        if (_loadoutManager != null)
            _loadoutManager.LoadoutReplaced -= BindLoadout;
        _loadoutManager = null;
        if (_loadout != null)
            _loadout.LoadoutChanged -= HandleLoadoutChanged;
        _loadout = null;
        Array.Clear(_slotIds, 0, _slotIds.Length);
        Array.Clear(_isNew, 0, _isNew.Length);

        // 확정하지 않은 변경은 버린다. 다시 열면 확정된 상태부터 시작한다.
        _hoveredTile = null;
        _isRefreshQueued = false;
        if (_tooltip != null)
        {
            _tooltip.Hide();
        }
    }

    private void OnDestroy()
    {
        if (_tiles != null)
        {
            foreach (RuneWordTileView tile in _tiles)
            {
                if (tile != null)
                {
                    tile.PointerEntered -= HandleTileEntered;
                    tile.PointerExited -= HandleTileExited;
                    tile.Clicked -= HandleTileClicked;
                }
            }
        }

        if (_sockets != null)
        {
            foreach (RuneSocketView socket in _sockets)
            {
                if (socket != null)
                {
                    socket.RightClicked -= HandleSocketRightClicked;
                }
            }
        }

        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_dataSource == null)
        {
            _dataSource = FindAnyObjectByType<RuneBookV2DataSource>();
        }

        if (_dataSource == null)
        {
            Debug.LogError("[RuneBookV2Presenter] RuneBookV2DataSource를 찾을 수 없습니다.", this);
        }

        _tiles ??= Array.Empty<RuneWordTileView>();
        _stoneCells ??= Array.Empty<RuneStoneCellView>();
        _sockets ??= Array.Empty<RuneSocketView>();

        foreach (RuneWordTileView tile in _tiles)
        {
            if (tile != null)
            {
                tile.PointerEntered += HandleTileEntered;
                tile.PointerExited += HandleTileExited;
                tile.Clicked += HandleTileClicked;
            }
        }

        for (int i = 0; i < _sockets.Length; i++)
        {
            if (_sockets[i] != null)
            {
                _sockets[i].Index = i;
                _sockets[i].RightClicked += HandleSocketRightClicked;
            }
        }

        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(HandleConfirmClicked);
        }
    }

    // ───────────────────────── 데이터 ─────────────────────────

    private void BindLoadoutManager()
    {
        if (_loadoutManager != null)
            _loadoutManager.LoadoutReplaced -= BindLoadout;
        _loadoutManager = RuneEffectLoadoutManager.Instance;
        if (_loadoutManager != null)
            _loadoutManager.LoadoutReplaced += BindLoadout;
        BindLoadout();
    }

    private void BindLoadout()
    {
        if (_loadout != null)
            _loadout.LoadoutChanged -= HandleLoadoutChanged;
        _loadout = _loadoutManager != null ? _loadoutManager.RuneEffectLoadout : null;
        if (_loadout != null)
            _loadout.LoadoutChanged += HandleLoadoutChanged;
        HandleLoadoutChanged();
    }

    private void HandleLoadoutChanged()
    {
        if (_dataSource == null)
            return;
        LoadData();
        LoadConfirmed();
        Refresh();
    }

    private void HandleDataChanged()
    {
        // 룬 문자 획득, 칸 확장 등. 확정 전 변경은 유지한다.
        LoadData();
        Refresh();
    }

    private void LoadData()
    {
        _entries.Clear();
        foreach (RuneBookEntryData entry in _dataSource.GetEntries())
        {
            if (entry != null)
            {
                _entries[entry.Id] = entry;
            }
        }

        _stoneList = _dataSource.GetRuneStones() ?? Array.Empty<RuneStoneStackData>();
        _stones.Clear();
        foreach (RuneStoneStackData stone in _stoneList)
        {
            if (stone != null)
            {
                _stones[stone.RuneCharacter] = stone;
            }
        }
    }

    private void LoadConfirmed()
    {
        for (int i = 0; i < _slotIds.Length; i++)
        {
            _slotIds[i] = i < ActiveSlotCount ? _loadout.GetEquippedEffectId(i) : null;
            _isNew[i] = false;
        }
    }

    // ───────────────────────── 입력 ─────────────────────────

    private void HandleTileClicked(RuneWordTileView tile)
    {
        RuneBookEntryData entry = tile.Entry;
        if (entry == null || !entry.IsRevealed || _dataSource == null)
        {
            return;
        }

        int slot = FindTargetSlot(entry, out string error);
        if (slot < 0)
        {
            ShowToast(error);
            return;
        }

        for (int i = ActiveSlotCount - 1; i > slot; i--)
        {
            _slotIds[i] = _slotIds[i - 1];
        }
        _slotIds[slot] = entry.Id;
        UpdateNewEquipmentFlags();
        Refresh();
    }

    private void HandleSocketRightClicked(RuneSocketView socket)
    {
        int index = socket.Index;
        if (index == RuneEffectLoadout.PrimaryElementSlotIndex)
            return;

        if (index < 0 || index >= ActiveSlotCount || _slotIds[index] == null)
        {
            return;
        }

        for (int i = index; i < ActiveSlotCount - 1; i++)
        {
            _slotIds[i] = _slotIds[i + 1];
        }
        _slotIds[ActiveSlotCount - 1] = null;
        UpdateNewEquipmentFlags();

        Refresh();
    }

    private void UpdateNewEquipmentFlags()
    {
        Dictionary<string, int> remainingCounts = new();

        for (int i = 0; i < _slotIds.Length; i++)
        {
            _isNew[i] = false;

            string effectId = _slotIds[i];
            if (string.IsNullOrEmpty(effectId))
                continue;

            if (!remainingCounts.TryGetValue(effectId, out int remainingCount))
                remainingCount = _loadout.GetEquippedCount(effectId);

            _isNew[i] = remainingCount == 0;
            remainingCounts[effectId] = Math.Max(0, remainingCount - 1);
        }
    }

    private void HandleTileEntered(RuneWordTileView tile)
    {
        _hoveredTile = tile;
        Refresh();
    }

    private void HandleTileExited(RuneWordTileView tile)
    {
        if (_hoveredTile != tile)
        {
            return;
        }

        _hoveredTile = null;
        if (_tooltip != null)
        {
            _tooltip.Hide();
        }

        // 칸이 꺼지는 도중(탭 전환)에도 불릴 수 있으므로 표시 갱신은 다음 LateUpdate로 미룬다.
        _isRefreshQueued = true;
    }

    private void HandleConfirmClicked()
    {
        if (_dataSource == null || _loadout == null)
        {
            return;
        }

        LoadData();
        CountPendingUse();
        if (HasShortage())
        {
            ShowToast(_notEnoughText);
            Refresh();
            return;
        }
        if (_dataSource.Confirm(_slotIds, _isNew))
        {
            _loadout.ConfirmEquipment(_slotIds);
            SceneLoader.LoadSpiritRealmScene();
        }
        else
        {
            LoadData();
            Refresh();
        }
    }

    // ───────────────────────── 규칙 ─────────────────────────

    /// <summary>룬 워드를 넣을 칸. 넣을 수 없으면 -1과 안내 문구.</summary>
    private int FindTargetSlot(RuneBookEntryData entry, out string error)
    {
        error = null;
        if (_loadout == null)
            return -1;
        int slot = _loadout.FindEquipSlot(_slotIds, entry.Id, out EquipError equipError);
        error = GetErrorText(equipError);
        return slot;
    }

    private string GetErrorText(EquipError error)
    {
        return error switch
        {
            EquipError.ElementLimit => _subElementLimitText,
            EquipError.SlotsFull => _slotsFullText,
            EquipError.CannotStack => _notStackableText,
            EquipError.MainElementRequired => _mainRequiredText,
            EquipError.NotEnoughRuneStones => _notEnoughText,
            _ => null
        };
    }

    /// <summary>
    /// 새로 넣으면 룬 문자를 쓰는지.
    /// 확정된 룬을 빼고 확정 전에 다시 넣으면 원래 있던 것이므로 쓰지 않는다.
    /// </summary>
    private bool IsAdditionalEquipment(string effectId)
    {
        int confirmedCount = _loadout.GetEquippedCount(effectId);
        int keptCount = _slotIds.Where((id, i) => id == effectId && !_isNew[i]).Count();

        return keptCount >= confirmedCount;
    }

    private bool HasShortage()
    {
        return !_dataSource.HasEnoughRuneStones(_pendingUse);
    }

    private bool CanConfirm()
    {
        return _loadout != null && !HasShortage();
    }

    // ───────────────────────── 표시 ─────────────────────────

    private void Refresh()
    {
        if (_dataSource == null)
        {
            return;
        }

        CountPendingUse();
        CountHoverUse();

        foreach (RuneWordTileView tile in _tiles)
        {
            if (tile != null)
            {
                _entries.TryGetValue(tile.EntryId, out RuneBookEntryData entry);
                tile.Show(entry, GetElementColor(entry));
            }
        }

        for (int i = 0; i < _stoneCells.Length; i++)
        {
            RuneStoneStackData stone = i < _stoneList.Count ? _stoneList[i] : null;
            if (_stoneCells[i] != null)
            {
                _stoneCells[i].Show(stone, stone != null ? Get(_pendingUse, stone.RuneCharacter) : 0,
                    stone != null ? Get(_hoverUse, stone.RuneCharacter) : 0);
            }
        }

        RefreshSockets();
        RefreshConfirm();
        RefreshTooltip();
    }

    private void RefreshSockets()
    {
        int active = ActiveSlotCount;
        for (int i = 0; i < _sockets.Length; i++)
        {
            RuneSocketView socket = _sockets[i];
            if (socket == null)
            {
                continue;
            }

            if (i >= active)
            {
                socket.Show(RuneSocketView.State.Locked, null, null);
                continue;
            }

            string id = _slotIds[i];
            if (id == null || !_entries.TryGetValue(id, out RuneBookEntryData entry))
            {
                socket.Show(RuneSocketView.State.Empty, null, null);
                continue;
            }

            socket.Show(_isNew[i] ? RuneSocketView.State.Pending : RuneSocketView.State.Equipped, entry.Icon,
                GetElementColor(entry));
        }

        if (_activatedText != null)
        {
            _activatedText.text = string.Format(_activatedFormat, active, RuneBookV2DataSource.SlotLimit);
        }
    }

    private void RefreshConfirm()
    {
        bool canConfirm = CanConfirm();
        if (_confirmButton != null)
        {
            _confirmButton.interactable = canConfirm;
        }

        if (_confirmGlow != null)
        {
            _confirmGlow.SetActive(canConfirm);
        }

        if (_confirmInfoText == null)
        {
            return;
        }

        int total = 0;
        foreach (int use in _pendingUse.Values)
        {
            total += use;
        }

        string info;
        bool isWarning = true;
        if (HasShortage())
        {
            info = _notEnoughText;
        }
        else
        {
            isWarning = false;
            info = total == 0 ? string.Empty : total == 1 ? _usesOneText : string.Format(_usesFormat, total);
        }

        _confirmInfoText.text = info;
        _confirmInfoText.color = isWarning ? _warningColor : _infoColor;
    }

    private void RefreshTooltip()
    {
        if (_tooltip == null)
        {
            return;
        }

        RuneBookEntryData entry = _hoveredTile != null ? _hoveredTile.Entry : null;
        if (entry == null)
        {
            _tooltip.Hide();
            return;
        }

        _tooltipStones.Clear();
        _tooltipMissing.Clear();
        if (entry.IsRevealed && !string.IsNullOrEmpty(entry.RuneWord))
        {
            foreach (char rune in entry.RuneWord)
            {
                int need = Count(entry.RuneWord, rune);
                bool isMissing = GetAvailable(rune) < need;
                Sprite icon = _stones.TryGetValue(rune, out RuneStoneStackData stone) ? stone.Icon : null;
                _tooltipStones.Add(new RuneWordTooltipView.RuneStoneView(icon, isMissing));
                if (isMissing && !_tooltipMissing.Contains(icon))
                {
                    _tooltipMissing.Add(icon);
                }
            }
        }

        _tooltip.Show(entry, _loadout != null && _loadout.CanOverlap(entry.Id), GetElementColor(entry), _tooltipStones,
            _tooltipMissing, _hoveredTile.RectTransform);
    }

    private void CountPendingUse()
    {
        _pendingUse.Clear();
        for (int i = 0; i < _slotIds.Length; i++)
        {
            if (_isNew[i] && _slotIds[i] != null && _entries.TryGetValue(_slotIds[i], out RuneBookEntryData entry))
            {
                AddUse(_pendingUse, entry.RuneWord);
            }
        }
    }

    private void CountHoverUse()
    {
        _hoverUse.Clear();
        RuneBookEntryData entry = _hoveredTile != null ? _hoveredTile.Entry : null;
        if (entry == null || !entry.IsRevealed || !IsAdditionalEquipment(entry.Id) || FindTargetSlot(entry, out _) < 0)
        {
            return;
        }

        AddUse(_hoverUse, entry.RuneWord);
    }

    private int GetAvailable(char rune)
    {
        int owned = _stones.TryGetValue(rune, out RuneStoneStackData stone) ? stone.Quantity : 0;
        return owned - Get(_pendingUse, rune);
    }

    private void ShowToast(string message)
    {
        if (_toast != null && !string.IsNullOrEmpty(message))
        {
            _toast.Show(message);
        }
    }

    private static Color? GetElementColor(RuneBookEntryData entry)
    {
        if (entry == null || entry.Category != RuneEffectCategory.Element ||
            !Enum.TryParse(entry.EnglishName, true, out ReactionElement element))
        {
            return null;
        }

        return ReactionElementStyle.GetColor(element);
    }

    private static void AddUse(Dictionary<char, int> uses, string runeWord)
    {
        if (string.IsNullOrEmpty(runeWord))
        {
            return;
        }

        foreach (char rune in runeWord)
        {
            uses[rune] = Get(uses, rune) + 1;
        }
    }

    private static int Get(Dictionary<char, int> uses, char rune)
    {
        return uses.TryGetValue(rune, out int value) ? value : 0;
    }

    private static int Count(string text, char value)
    {
        int count = 0;
        foreach (char c in text)
        {
            if (c == value)
            {
                count++;
            }
        }

        return count;
    }
}
