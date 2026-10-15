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
/// - 원소는 최소 1개, 최대 2개. 마지막 원소도 오른쪽 클릭으로 뺄 수는 있지만, 경고를 띄우고
///   원소가 하나도 없으면 Confirm해도 확정하지 않는다. ("원소는 최소 1개 장착해야 합니다")
///   MAIN 원소를 빼면 보조 원소가 MAIN으로 올라온다. 원소를 넣을 때 MAIN이 비어 있으면 MAIN에 넣는다.
///   (시작 석상에서 처음 고르는 주 원소만 무료이고, 이후 새로 넣는 원소는 다른 룬처럼 룬 문자를 쓴다)
/// - 서브 원소는 1개까지(MAIN과 같은 원소도 가능). 두 번째를 넣으면 "보조 원소는 하나만 장착할 수 있습니다".
/// - 원소가 아닌 룬은 마지막 장착 룬 뒤에 들어간다. 빈 칸이 없으면 "모든 칸이 찼습니다".
/// - 겹치기가 안 되는 룬을 이미 넣었으면 "이 룬은 중첩할 수 없습니다".
/// - 장착 칸을 오른쪽 클릭하면 뺀다. 이미 확정된 룬을 빼도 룬 문자는 돌려주지 않는다.
/// - 룬 문자가 모자라면(남는 수가 0 미만) Confirm을 막고 "룬석이 부족합니다"를 띄운다.
/// - Confirm 없이 룬북을 닫으면(이 오브젝트가 꺼지면) 바꾼 내용은 버린다.
/// - 확인 버튼(또는 F, Space)을 누르면 확정 확인 창(RuneBookConfirmDialog)을 띄우고, 거기서 확정해야 실제로 확정한다.
///   한 번 확정한 뒤에는 칸을 다시 바꾸기 전까지 확인 버튼을 받지 않는다. (연타로 확인 창이 다시 뜨지 않게)
/// - 아래 띠의 "활성화 n / m"은 장착한 룬 수 / 열린 칸 수다.
/// - 연출(열기, 장착/해제, 경고, 룬석 부족, Confirm)은 RuneBookV2Fx가 맡는다. 없으면 붙인다.
///
/// 보기 전용 (보상 선택 중 Tab으로 연 룬북)
/// - 장착을 바꾸거나 Confirm할 수 없다. 확인 버튼 대신 청동 봉인 "돌아가기 [TAB]"(_viewOnlyCloseButton)과 안내 문구,
///   "열람 중" 책갈피(_viewOnlyBadge)를 보여준다. 그림은 모두 프리팹에 할당한 이미지다.
/// - 칸이나 룬 워드를 누르면 그 칸이 짧게 떨고 안내 문구가 붉게 반짝인다.
/// - 돌아가기 봉인을 누르면 ViewOnlyCloseRequested를 보낸다. (RuneBookV2Toggle이 받아 룬북을 닫는다)
///
/// 완성품 (상점에서 산 룬 워드)
/// - 완성품이 있는 룬 워드를 새로 넣으면 룬 문자 대신 완성품을 쓴다. (앞 칸부터, 보유 수만큼)
/// - Confirm할 때 완성품으로 넣은 칸마다 완성품을 1개 소모한다.
/// - 칸에는 남은 완성품 수(x1)를, 팝업에는 "Owned x1 - No rune stones needed"를 보여준다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookV2Presenter : MonoBehaviour
{
    [SerializeField] private RuneBookV2DataSource _dataSource;

    [Tooltip("Confirm 후 전투 씬으로 넘어간다 (거점의 출발 화면). 전투 중 Tab 룬북은 끈다.")]
    [SerializeField] private bool _loadCombatSceneOnConfirm = true;
    [Tooltip("확정 확인 창 프리팹 (RuneBookConfirmDialog). 비워두면 확인 없이 바로 확정한다")]
    [SerializeField] private RuneBookConfirmDialog _confirmDialogPrefab;

    [Header("보기 전용")]
    [Tooltip("보기 전용으로 열었을 때 보여줄 안내 (보상 선택 중 Tab으로 연 룬북)")]
    [SerializeField] private string _viewOnlyText = "보상 선택 중에는 장착을 바꿀 수 없어요";
    [Tooltip("보기 전용에서 확인 버튼 대신 보여줄 닫기 버튼 (청동 봉인 \"돌아가기 [TAB]\"). 꺼진 상태로 둔다")]
    [SerializeField] private Button _viewOnlyCloseButton;
    [Tooltip("보기 전용 안내 문구. 비워두면 확인 버튼의 안내 문구(_confirmInfoText)를 쓴다")]
    [SerializeField] private TMP_Text _viewOnlyInfoText;
    [Tooltip("보기 전용일 때만 켜는 표시 (\"열람 중\" 책갈피). 꺼진 상태로 둔다")]
    [SerializeField] private GameObject _viewOnlyBadge;
    [Tooltip("보기 전용에서 칸을 눌렀을 때 안내 문구가 반짝이는 색")]
    [SerializeField] private Color _viewOnlyFlashColor = new(0.86f, 0.28f, 0.18f, 1f);

    [Header("Rune Words")]
    [SerializeField] private RuneWordTileView[] _tiles;
    [SerializeField] private RuneWordTooltipView _tooltip;

    [Header("Rune Stones (표시 순서대로)")]
    [SerializeField] private RuneStoneCellView[] _stoneCells;

    [Header("Activated")]
    [SerializeField] private RuneSocketView[] _sockets;
    [SerializeField] private TMP_Text _activatedText;
    [SerializeField] private string _activatedFormat = "활성화  {0} / {1}";

    [Header("확정 버튼")]
    [SerializeField] private Button _confirmButton;
    [SerializeField] private GameObject _confirmGlow;
    [SerializeField] private TMP_Text _confirmInfoText;
    [SerializeField] private Color _infoColor = new(0.941f, 0.745f, 0.431f, 1f);
    [SerializeField] private Color _warningColor = new(0.941f, 0.471f, 0.392f, 1f);

    [Header("안내")]
    [SerializeField] private RuneBookToastView _toast;

    [Header("문구")]
    [SerializeField] private string _usesFormat = "룬석 {0}개 사용";
    [SerializeField] private string _usesOneText = "룬석 1개 사용";
    [SerializeField] private string _usesOwnedText = "보유한 룬 워드 사용";
    [SerializeField] private string _notEnoughText = "룬석이 부족합니다";
    [SerializeField] private string _mainRequiredText = "주력 원소를 먼저 장착하세요";
    [SerializeField] private string _elementRequiredText = "원소는 최소 1개 장착해야 합니다";
    [SerializeField] private string _subElementLimitText = "보조 원소는 하나만 장착할 수 있습니다";
    [SerializeField] private string _slotsFullText = "모든 칸이 찼습니다";
    [SerializeField] private string _notStackableText = "이 룬은 중첩할 수 없습니다";

    private readonly string[] _slotIds = new string[RuneBookV2DataSource.SlotLimit];
    private readonly bool[] _isNew = new bool[RuneBookV2DataSource.SlotLimit];
    // 새로 넣은 칸 중 룬 문자 대신 완성품을 쓰는 칸
    private readonly bool[] _usesOwned = new bool[RuneBookV2DataSource.SlotLimit];
    private readonly Dictionary<string, int> _owned = new();
    private readonly Dictionary<string, RuneBookEntryData> _entries = new();
    private readonly Dictionary<char, RuneStoneStackData> _stones = new();
    private readonly Dictionary<char, int> _pendingUse = new();
    private readonly Dictionary<char, int> _hoverUse = new();
    private readonly List<RuneWordTooltipView.RuneStoneView> _tooltipStones = new();
    private readonly List<Sprite> _tooltipMissing = new();
    private IReadOnlyList<RuneStoneStackData> _stoneList = Array.Empty<RuneStoneStackData>();
    private RuneWordTileView _hoveredTile;
    // 마우스가 올라가 있는 장착 칸 (룬이 있는 칸이면 목록과 같은 툴팁을 띄운다)
    private RuneSocketView _hoveredSocket;
    private bool _isRefreshQueued;
    private int _enabledFrame = -1;
    private RuneBookConfirmDialog _confirmDialog;
    private bool _isInitialized;
    private PlayerRuneManager _loadoutManager;
    private RuneEffectLoadout _loadout;
    private RuneBookV2Fx _fx;

    // 로드아웃(PlayerRuneManager)이 없는 씬(RuneBookV2Scene 단독 테스트)에서는 데이터 소스의 값과 규칙을 쓴다.
    private int ActiveSlotCount => _loadout != null
        ? _loadout.ActiveSlotCount
        : _dataSource != null ? Mathf.Clamp(_dataSource.ActiveSlotCount, 1, RuneBookV2DataSource.SlotLimit) : 0;

    private bool _isViewOnly;

    // 확정한 뒤 칸을 다시 바꾸기 전까지 확인 버튼(F, Space 포함)을 받지 않는다.
    private bool _isConfirmLocked;

    /// <summary>보기 전용이면 룬 워드를 넣거나 빼거나 Confirm할 수 없다.</summary>
    public bool IsViewOnly => _isViewOnly;

    /// <summary>지금 룬북 칸에 놓인 룬 효과 ID (Confirm 전 변경 포함, 빈 칸은 null). Reactions 탭이 장착 조합을 강조할 때 쓴다.</summary>
    public IReadOnlyList<string> SlotIds => _slotIds;

    /// <summary>Confirm으로 장착을 확정했을 때 (시작 석상이 받아 출발 연출을 시작한다)</summary>
    public event Action Confirmed;

    /// <summary>보기 전용에서 돌아가기 봉인을 눌렀을 때 (RuneBookV2Toggle이 받아 룬북을 닫는다)</summary>
    public event Action ViewOnlyCloseRequested;

    private TMP_Text ViewOnlyInfoText => _viewOnlyInfoText != null ? _viewOnlyInfoText : _confirmInfoText;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 보기 전용으로 바꾼다. (보상 선택 중 Tab으로 연 룬북)
    /// 보기 전용에서는 Confirm 버튼 대신 돌아가기 봉인을 보여주고 장착 변경을 막는다. 툴팁은 그대로 보인다.
    /// </summary>
    public void SetViewOnly(bool isViewOnly)
    {
        _isViewOnly = isViewOnly;
        _isRefreshQueued = true;
    }

    private void OnEnable()
    {
        _enabledFrame = Time.frameCount;
        _isConfirmLocked = false;
        Initialize();
        _fx.PlayOpen();
        if (_dataSource == null)
        {
            return;
        }

        _dataSource.Changed += HandleDataChanged;
        BindLoadoutManager();
        _isRefreshQueued = true;
    }

    /// <summary>F 또는 Space로 확인 버튼을 누른다. (확인 창이 열려 있거나 방금 닫힌 프레임, 룬북을 연 직후에는 받지 않는다)</summary>
    private void Update()
    {
        if (_isViewOnly || RuneBookConfirmDialog.IsOpen || RuneBookConfirmDialog.ClosedFrame == Time.frameCount ||
            Time.frameCount <= _enabledFrame + 1)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.F) && !Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        if (_confirmButton != null && _confirmButton.gameObject.activeInHierarchy && _confirmButton.interactable)
        {
            HandleConfirmClicked();
        }
    }

    private void LateUpdate()
    {
        if (_loadoutManager != PlayerRuneManager.Instance)
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
        // 룬북이 닫히면 열려 있던 확인 창도 확정 없이 닫는다.
        if (_confirmDialog != null)
        {
            _confirmDialog.Hide();
        }
        Array.Clear(_slotIds, 0, _slotIds.Length);
        Array.Clear(_isNew, 0, _isNew.Length);
        Array.Clear(_usesOwned, 0, _usesOwned.Length);

        // 확정하지 않은 변경은 버린다. 다시 열면 확정된 상태부터 시작한다.
        _hoveredTile = null;
        _hoveredSocket = null;
        _isRefreshQueued = false;
        if (_tooltip != null)
        {
            _tooltip.Hide();
        }
    }

    private void OnDestroy()
    {
        if (_confirmDialog != null)
        {
            Destroy(_confirmDialog.gameObject);
        }

        if (_tiles != null)
        {
            foreach (RuneWordTileView tile in _tiles)
            {
                if (tile != null)
                {
                    tile.PointerEntered -= HandleTileEntered;
                    tile.PointerExited -= HandleTileExited;
                    tile.Clicked -= HandleTileClicked;
                    tile.RightClicked -= HandleTileRightClicked;
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
                    socket.PointerEntered -= HandleSocketEntered;
                    socket.PointerExited -= HandleSocketExited;
                }
            }
        }

        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        if (_viewOnlyCloseButton != null)
        {
            _viewOnlyCloseButton.onClick.RemoveListener(HandleViewOnlyCloseClicked);
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _fx = GetComponent<RuneBookV2Fx>();
        if (_fx == null)
        {
            _fx = gameObject.AddComponent<RuneBookV2Fx>();
        }

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
                tile.RightClicked += HandleTileRightClicked;
            }
        }

        for (int i = 0; i < _sockets.Length; i++)
        {
            if (_sockets[i] != null)
            {
                _sockets[i].Index = i;
                _sockets[i].RightClicked += HandleSocketRightClicked;
                _sockets[i].PointerEntered += HandleSocketEntered;
                _sockets[i].PointerExited += HandleSocketExited;
            }
        }

        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (_viewOnlyCloseButton != null)
        {
            _viewOnlyCloseButton.onClick.AddListener(HandleViewOnlyCloseClicked);
        }
    }

    // ───────────────────────── 데이터 ─────────────────────────

    private void BindLoadoutManager()
    {
        if (_loadoutManager != null)
            _loadoutManager.LoadoutReplaced -= BindLoadout;
        _loadoutManager = PlayerRuneManager.Instance;
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
        HandleLoadoutChanged(_loadout?.EquippedEffectIds);
    }

    private void HandleLoadoutChanged(IReadOnlyList<string> equippedEffectIds)
    {
        if (_dataSource == null)
            return;
        LoadData();
        LoadConfirmed();
        Refresh();
    }

    private void HandleDataChanged()
    {
        // 룬 문자 획득, 칸 확장, 완성품 수 변경 등. 확정 전 변경은 유지한다.
        LoadData();
        UpdateOwnedFlags();
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

        _owned.Clear();
        foreach (string id in _entries.Keys)
        {
            int count = _dataSource.GetOwnedCount(id);
            if (count > 0)
            {
                _owned[id] = count;
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
            _slotIds[i] = i < ActiveSlotCount ? GetConfirmedId(i) : null;
            _isNew[i] = false;
            _usesOwned[i] = false;
        }
    }

    // ───────────────────────── 입력 ─────────────────────────

    private void HandleTileClicked(RuneWordTileView tile)
    {
        if (_isViewOnly)
        {
            PlayViewOnlyBlocked(tile);
            return;
        }

        RuneBookEntryData entry = tile.Entry;
        if (entry == null || _dataSource == null)
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
        _isConfirmLocked = false;
        UpdateNewEquipmentFlags();
        Dictionary<char, int> usedBefore = new(_pendingUse);
        Refresh();

        // 연출 : 들어간 칸과 새로 쓰게 될 룬석 칸
        if (slot < _sockets.Length)
        {
            _fx.PlayEquip(_sockets[slot], GetElementColor(entry));
        }

        for (int i = 0; i < _stoneCells.Length && i < _stoneList.Count; i++)
        {
            RuneStoneStackData stone = _stoneList[i];
            if (stone != null && Get(_pendingUse, stone.RuneCharacter) > Get(usedBefore, stone.RuneCharacter))
            {
                _fx.PlayStoneUse(_stoneCells[i]);
            }
        }
    }

    private void HandleSocketRightClicked(RuneSocketView socket)
    {
        if (_isViewOnly)
        {
            PlayViewOnlyBlocked(socket);
            return;
        }

        RemoveSlot(socket.Index);
    }

    /// <summary>
    /// 룬 목록에서 우클릭 : 장착 칸에 들어 있는 이 룬 워드를 뺀다. (장착 칸으로 마우스를 옮기지 않아도 된다)
    /// 같은 룬 워드가 여러 칸에 있으면 가장 뒤 칸부터 하나씩 뺀다.
    /// </summary>
    private void HandleTileRightClicked(RuneWordTileView tile)
    {
        if (_isViewOnly)
        {
            PlayViewOnlyBlocked(tile);
            return;
        }

        if (tile.Entry == null)
        {
            return;
        }

        for (int i = ActiveSlotCount - 1; i >= 0; i--)
        {
            if (_slotIds[i] == tile.Entry.Id)
            {
                RemoveSlot(i);
                return;
            }
        }
    }

    /// <summary>장착 칸 index의 룬 워드를 빼고 뒤 칸을 앞으로 당긴다.</summary>
    private void RemoveSlot(int index)
    {
        if (index < 0 || index >= ActiveSlotCount || _slotIds[index] == null)
        {
            return;
        }

        for (int i = index; i < ActiveSlotCount - 1; i++)
        {
            _slotIds[i] = _slotIds[i + 1];
        }
        _slotIds[ActiveSlotCount - 1] = null;
        _isConfirmLocked = false;
        UpdateNewEquipmentFlags();

        // 원소는 최소 1개 : 빼는 건 허용하고 경고만 띄운다. (Confirm에서 막는다)
        Refresh();
        if (index < _sockets.Length && _sockets[index] != null)
        {
            _fx.PlayRemove(_sockets[index]);
        }

        if (!HasElementInSlots())
        {
            ShowToast(_elementRequiredText);
            PlayElementWarning();
        }
    }

    private void PlayElementWarning()
    {
        if (_sockets.Length > RuneEffectLoadout.PrimaryElementSlotIndex)
        {
            _fx.PlayWarning(_sockets[RuneEffectLoadout.PrimaryElementSlotIndex]);
        }
    }

    private void PlayShortage()
    {
        for (int i = 0; i < _stoneCells.Length && i < _stoneList.Count; i++)
        {
            RuneStoneStackData stone = _stoneList[i];
            if (stone != null && _stoneCells[i] != null &&
                stone.Quantity - Get(_pendingUse, stone.RuneCharacter) < 0)
            {
                _fx.PlayStoneShortage(_stoneCells[i], _stoneCells[i].FlashSprite);
            }
        }
    }

    private void PlayConfirmed()
    {
        List<RuneSocketView> sockets = new();
        List<Color?> colors = new();
        for (int i = 0; i < ActiveSlotCount && i < _sockets.Length; i++)
        {
            string id = _slotIds[i];
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            sockets.Add(_sockets[i]);
            colors.Add(_entries.TryGetValue(id, out RuneBookEntryData entry) ? GetElementColor(entry) : null);
        }

        _fx.PlayConfirm(sockets, colors);
    }

    private bool IsElement(string effectId)
    {
        return !string.IsNullOrEmpty(effectId) &&
               _entries.TryGetValue(effectId, out RuneBookEntryData entry) &&
               entry.Category == RuneEffectCategory.Element;
    }

    private bool HasElementInSlots()
    {
        for (int i = 0; i < ActiveSlotCount; i++)
        {
            if (IsElement(_slotIds[i]))
            {
                return true;
            }
        }

        return false;
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
                remainingCount = GetConfirmedCount(effectId);

            _isNew[i] = remainingCount == 0;
            remainingCounts[effectId] = Math.Max(0, remainingCount - 1);
        }

        UpdateOwnedFlags();
    }

    /// <summary>새로 넣은 칸에 앞에서부터 완성품을 배정한다. (룬 워드별 보유 수만큼)</summary>
    private void UpdateOwnedFlags()
    {
        for (int i = 0; i < _slotIds.Length; i++)
        {
            _usesOwned[i] = _isNew[i] && !string.IsNullOrEmpty(_slotIds[i]) &&
                            CountUsedOwned(_slotIds[i], i) < GetOwned(_slotIds[i]);
        }
    }

    /// <summary>앞의 칸들(beforeIndex 전)에서 완성품을 쓰는 수</summary>
    private int CountUsedOwned(string effectId, int beforeIndex = RuneBookV2DataSource.SlotLimit)
    {
        int count = 0;
        for (int i = 0; i < beforeIndex && i < _slotIds.Length; i++)
        {
            if (_usesOwned[i] && _slotIds[i] == effectId)
            {
                count++;
            }
        }

        return count;
    }

    private int GetOwned(string effectId)
    {
        return effectId != null && _owned.TryGetValue(effectId, out int count) ? count : 0;
    }

    /// <summary>아직 칸에 배정하지 않은 완성품 수</summary>
    private int GetRemainingOwned(string effectId)
    {
        return Math.Max(0, GetOwned(effectId) - CountUsedOwned(effectId));
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

    private void HandleSocketEntered(RuneSocketView socket)
    {
        _hoveredSocket = socket;
        _hoveredTile = null;
        Refresh();
    }

    private void HandleSocketExited(RuneSocketView socket)
    {
        if (_hoveredSocket != socket)
        {
            return;
        }

        _hoveredSocket = null;
        if (_tooltip != null)
        {
            _tooltip.Hide();
        }
    }

    private void HandleViewOnlyCloseClicked()
    {
        if (_isViewOnly)
        {
            ViewOnlyCloseRequested?.Invoke();
        }
    }

    /// <summary>보기 전용에서 칸을 눌렀을 때 : 칸이 짧게 떨고 안내 문구가 반짝인다.</summary>
    private void PlayViewOnlyBlocked(Component target)
    {
        _fx.PlayViewOnlyBlocked(target, ViewOnlyInfoText, _viewOnlyFlashColor);
    }

    private void HandleConfirmClicked()
    {
        if (_dataSource == null || _isViewOnly || _isConfirmLocked)
        {
            return;
        }

        if (!HasElementInSlots())
        {
            ShowToast(_elementRequiredText);
            Refresh();
            PlayElementWarning();
            return;
        }

        LoadData();
        UpdateOwnedFlags();
        CountPendingUse();
        if (HasShortage())
        {
            ShowToast(_notEnoughText);
            Refresh();
            PlayShortage();
            return;
        }

        // 확인 창에서 확정을 고르면 실제로 확정한다.
        ShowConfirmDialog();
    }

    /// <summary>확정 확인 창을 띄운다. 처음 한 번 프리팹으로 만든다. 프리팹이 없으면 바로 확정한다.</summary>
    private void ShowConfirmDialog()
    {
        if (_confirmDialogPrefab == null)
        {
            ConfirmLoadout();
            return;
        }

        if (_confirmDialog == null)
        {
            // 룬북과 따로 둔다. (룬북이 꺼질 때 같이 꺼지지 않고, 자기 캔버스로 룬북 위에 그려진다)
            _confirmDialog = Instantiate(_confirmDialogPrefab);
            _confirmDialog.name = _confirmDialogPrefab.name;
            _confirmDialog.gameObject.SetActive(false);
        }

        _confirmDialog.Show(ConfirmLoadout);
    }

    /// <summary>확인 창에서 확정했을 때 : 룬 문자와 완성품을 쓰고 장착을 확정한다.</summary>
    private void ConfirmLoadout()
    {
        if (_dataSource == null || _isViewOnly || !isActiveAndEnabled)
        {
            return;
        }

        // 확인 창이 떠 있던 사이에 바뀌었을 수 있으니 다시 검사한다.
        LoadData();
        UpdateOwnedFlags();
        CountPendingUse();
        if (!HasElementInSlots() || HasShortage())
        {
            Refresh();
            return;
        }

        // 룬 문자는 완성품을 쓰지 않는 새 칸만 소모한다.
        bool[] usesRuneStones = new bool[_slotIds.Length];
        List<string> ownedToConsume = new();
        for (int i = 0; i < _slotIds.Length; i++)
        {
            usesRuneStones[i] = _isNew[i] && !_usesOwned[i];
            if (_isNew[i] && _usesOwned[i] && _slotIds[i] != null)
            {
                ownedToConsume.Add(_slotIds[i]);
            }
        }

        if (_dataSource.Confirm(_slotIds, usesRuneStones))
        {
            _isConfirmLocked = true;
            foreach (string id in ownedToConsume)
            {
                _dataSource.ConsumeOwned(id);
            }

            PlayConfirmed();

            if (_loadout != null)
            {
                _loadout.ConfirmEquipment(_slotIds);
                Confirmed?.Invoke();
                if (_loadCombatSceneOnConfirm)
                {
                    // 마을에서 출발 : 원정(지갑, 시작 골드)을 시작하고 전투 씬으로 간다.
                    RunFlow.StartExpedition();
                    SceneLoader.LoadCombatSceneScene();
                }
                else
                {
                    // 전투 중 : 씬은 그대로 두고 확정된 상태로 다시 그린다.
                    LoadData();
                    LoadConfirmed();
                    Refresh();
                }
            }
            else
            {
                // 단독 테스트 : 데이터 소스에만 확정하고 화면을 다시 그린다.
                LoadData();
                LoadConfirmed();
                Refresh();
            }
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
        int slot = _loadout != null
            ? _loadout.FindEquipSlot(_slotIds, entry.Id, out EquipError equipError)
            : FindEquipSlotWithoutLoadout(entry, out equipError);
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
            EquipError.ConflictingEndEffect => "Return, Orbit, Fix는 함께 장착할 수 없습니다.",
            _ => null
        };
    }

    /// <summary>
    /// 새로 넣으면 룬 문자를 쓰는지.
    /// 확정된 룬을 빼고 확정 전에 다시 넣으면 원래 있던 것이므로 쓰지 않는다.
    /// </summary>
    private bool IsAdditionalEquipment(string effectId)
    {
        int confirmedCount = GetConfirmedCount(effectId);
        int keptCount = _slotIds.Where((id, i) => id == effectId && !_isNew[i]).Count();

        return keptCount >= confirmedCount;
    }

    private bool HasShortage()
    {
        return !_dataSource.HasEnoughRuneStones(_pendingUse);
    }

    private bool CanConfirm()
    {
        return _dataSource != null && !HasShortage();
    }

    private string GetConfirmedId(int slotIndex)
    {
        if (_loadout != null)
        {
            return _loadout.GetEquippedEffectId(slotIndex);
        }

        return _dataSource != null ? _dataSource.GetConfirmedId(slotIndex) : null;
    }

    private int GetConfirmedCount(string effectId)
    {
        if (_loadout != null)
        {
            return _loadout.GetEquippedCount(effectId);
        }

        int count = 0;
        for (int i = 0; i < ActiveSlotCount; i++)
        {
            if (GetConfirmedId(i) == effectId)
            {
                count++;
            }
        }

        return count;
    }

    private bool CanOverlap(string effectId)
    {
        if (_loadout != null)
        {
            return _loadout.CanOverlap(effectId);
        }

        bool isElement = _entries.TryGetValue(effectId, out RuneBookEntryData entry) &&
                         entry.Category == RuneEffectCategory.Element;
        return isElement || (_dataSource != null && _dataSource.CanOverlap(effectId));
    }

    /// <summary>
    /// 로드아웃이 없을 때 RuneEffectLoadout.FindEquipSlot과 같은 규칙으로 넣을 칸을 정한다.
    /// (원소는 앞의 두 칸에 순서대로, 서브 원소는 1개까지, 비원소는 마지막 장착 룬 뒤)
    /// </summary>
    private int FindEquipSlotWithoutLoadout(RuneBookEntryData entry, out EquipError error)
    {
        int occupied = 0;
        int elements = 0;
        foreach (string id in _slotIds)
        {
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            occupied++;
            if (_entries.TryGetValue(id, out RuneBookEntryData equipped) &&
                equipped.Category == RuneEffectCategory.Element)
            {
                elements++;
            }
        }

        error = EquipError.None;
        bool isElement = entry.Category == RuneEffectCategory.Element;

        if (RuneEffectLoadout.HasConflictingEndEffect(_slotIds, entry.Id))
        {
            error = EquipError.ConflictingEndEffect;
        }
        else if (Array.IndexOf(_slotIds, entry.Id) >= 0 && !CanOverlap(entry.Id))
        {
            error = EquipError.CannotStack;
        }
        else if (isElement && elements > RuneEffectLoadout.ElementRuneEffectSlotIndex)
        {
            error = EquipError.ElementLimit;
        }
        else if (occupied >= ActiveSlotCount)
        {
            error = EquipError.SlotsFull;
        }

        if (error != EquipError.None)
        {
            return -1;
        }

        return isElement ? elements : occupied;
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
                tile.Show(entry, GetElementColor(entry), entry != null ? GetRemainingOwned(entry.Id) : 0,
                    entry != null && IsInSlots(entry.Id));
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
            // 장착한 룬 수 / 열린 칸 수
            int equipped = 0;
            for (int i = 0; i < active && i < _slotIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(_slotIds[i]))
                {
                    equipped++;
                }
            }

            _activatedText.text = string.Format(_activatedFormat, equipped, active);
        }
    }

    private void RefreshConfirm()
    {
        if (_confirmButton != null)
        {
            _confirmButton.gameObject.SetActive(!_isViewOnly);
        }

        if (_viewOnlyCloseButton != null)
        {
            _viewOnlyCloseButton.gameObject.SetActive(_isViewOnly);
        }

        if (_viewOnlyBadge != null)
        {
            _viewOnlyBadge.SetActive(_isViewOnly);
        }

        if (_isViewOnly)
        {
            if (_confirmGlow != null)
            {
                _confirmGlow.SetActive(false);
            }

            TMP_Text infoText = ViewOnlyInfoText;
            if (infoText != null)
            {
                infoText.text = _viewOnlyText;
                infoText.color = _infoColor;
            }

            return;
        }

        bool canConfirm = CanConfirm();
        if (_confirmButton != null)
        {
            _confirmButton.interactable = canConfirm;
        }

        if (_confirmGlow != null)
        {
            _confirmGlow.SetActive(canConfirm && HasElementInSlots());
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
        if (!HasElementInSlots())
        {
            info = _elementRequiredText;
        }
        else if (HasShortage())
        {
            info = _notEnoughText;
        }
        else
        {
            isWarning = false;
            bool usesOwned = Array.IndexOf(_usesOwned, true) >= 0;
            info = total == 0 ? usesOwned ? _usesOwnedText : string.Empty
                : total == 1 ? _usesOneText : string.Format(_usesFormat, total);
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

        if (_hoveredTile == null && _hoveredSocket != null)
        {
            ShowSocketTooltip(_hoveredSocket);
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
        int ownedCount = GetRemainingOwned(entry.Id);
        if (!string.IsNullOrEmpty(entry.RuneWord))
        {
            foreach (char rune in entry.RuneWord)
            {
                int need = Count(entry.RuneWord, rune);
                // 완성품이 있으면 룬 문자가 필요 없으므로 모자란 표시를 하지 않는다.
                bool isMissing = ownedCount <= 0 && GetAvailable(rune) < need;
                Sprite icon = _stones.TryGetValue(rune, out RuneStoneStackData stone) ? stone.Icon : null;
                _tooltipStones.Add(new RuneWordTooltipView.RuneStoneView(icon, isMissing));
                if (isMissing && !_tooltipMissing.Contains(icon))
                {
                    _tooltipMissing.Add(icon);
                }
            }
        }

        _tooltip.Show(entry, CanOverlap(entry.Id), GetElementColor(entry), _tooltipStones,
            _tooltipMissing, _hoveredTile.RectTransform, ownedCount);
    }

    /// <summary>
    /// 장착 칸에 놓인 룬의 툴팁. 이미 칸에 들어간 룬이므로 모자란 룬 문자와 완성품 수는 표시하지 않는다.
    /// 빈 칸이나 잠긴 칸이면 숨긴다.
    /// </summary>
    private void ShowSocketTooltip(RuneSocketView socket)
    {
        int index = socket.Index;
        string id = index >= 0 && index < ActiveSlotCount && index < _slotIds.Length ? _slotIds[index] : null;
        if (id == null || !_entries.TryGetValue(id, out RuneBookEntryData entry))
        {
            _tooltip.Hide();
            return;
        }

        _tooltipStones.Clear();
        _tooltipMissing.Clear();
        if (!string.IsNullOrEmpty(entry.RuneWord))
        {
            foreach (char rune in entry.RuneWord)
            {
                Sprite icon = _stones.TryGetValue(rune, out RuneStoneStackData stone) ? stone.Icon : null;
                _tooltipStones.Add(new RuneWordTooltipView.RuneStoneView(icon, false));
            }
        }

        _tooltip.Show(entry, CanOverlap(entry.Id), GetElementColor(entry), _tooltipStones,
            _tooltipMissing, socket.RectTransform);
    }

    private void CountPendingUse()
    {
        _pendingUse.Clear();
        for (int i = 0; i < _slotIds.Length; i++)
        {
            if (_isNew[i] && !_usesOwned[i] && _slotIds[i] != null &&
                _entries.TryGetValue(_slotIds[i], out RuneBookEntryData entry))
            {
                AddUse(_pendingUse, entry.RuneWord);
            }
        }
    }

    private void CountHoverUse()
    {
        _hoverUse.Clear();
        RuneBookEntryData entry = _hoveredTile != null ? _hoveredTile.Entry : null;
        if (entry == null || !IsAdditionalEquipment(entry.Id) || FindTargetSlot(entry, out _) < 0)
        {
            return;
        }

        // 남은 완성품이 있으면 룬 문자를 쓰지 않는다.
        if (GetRemainingOwned(entry.Id) > 0)
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

    private bool IsInSlots(string id)
    {
        return Array.IndexOf(_slotIds, id) >= 0;
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
