using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룬북 화면 전체를 관리한다.
/// 데이터 소스의 내용을 각 View에 전달하고, 칸 선택과 활성화/비활성화 버튼 입력을 데이터 소스에 요청한다.
/// 사용 흐름: 아래 활성 칸 선택 → 룬 목록에서 룬 선택 → 활성화 (칸이 차 있으면 교체)
/// </summary>
[DisallowMultipleComponent]
public class RuneBookPresenter : MonoBehaviour
{
    private const int NoSelection = -1;

    [Header("데이터")]
    [SerializeField] private RuneBookDataSource _dataSource;

    [Header("룬 목록 칸의 부모 (RuneSymbolPart)")]
    [SerializeField] private Transform _entrySlotContainer;

    [Header("활성 룬 칸의 부모 (ActivatedRunes)")]
    [SerializeField] private Transform _activatedSlotContainer;

    [Header("상세")]
    [SerializeField] private RuneDetailView _detailView;
    [SerializeField] private Button _activateButton;
    [SerializeField] private Button _deactivateButton;

    [Header("보유 룬석")]
    [SerializeField] private OwnedRuneStoneListView _ownedRuneStoneList;

    private RuneSymbolSlotView[] _entrySlots = Array.Empty<RuneSymbolSlotView>();
    private RuneSymbolSlotView[] _activatedSlots = Array.Empty<RuneSymbolSlotView>();
    private IReadOnlyList<RuneBookEntryData> _entries = Array.Empty<RuneBookEntryData>();
    private RuneBookDataSource _subscribedDataSource;
    private int _selectedEntryIndex = NoSelection;
    private int _selectedSlotIndex;
    private bool _isInitialized;
    private bool _hasWarnedSlotShortage;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        Subscribe(_dataSource);
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        if (_activateButton != null)
        {
            _activateButton.onClick.RemoveListener(HandleActivateClicked);
        }

        if (_deactivateButton != null)
        {
            _deactivateButton.onClick.RemoveListener(HandleDeactivateClicked);
        }

        foreach (RuneSymbolSlotView slot in _entrySlots)
        {
            slot.Clicked -= HandleEntrySlotClicked;
        }

        foreach (RuneSymbolSlotView slot in _activatedSlots)
        {
            slot.Clicked -= HandleActivatedSlotClicked;
        }
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    /// <summary>
    /// 실행 중에 데이터 소스를 교체할 때 사용한다.
    /// </summary>
    public void SetDataSource(RuneBookDataSource dataSource)
    {
        _dataSource = dataSource;
        _selectedEntryIndex = NoSelection;
        _selectedSlotIndex = 0;

        if (isActiveAndEnabled)
        {
            Subscribe(_dataSource);
            Refresh();
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        ValidateReferences();

        _entrySlots = CollectSlots(_entrySlotContainer, HandleEntrySlotClicked);
        _activatedSlots = CollectSlots(_activatedSlotContainer, HandleActivatedSlotClicked);

        if (_activateButton != null)
        {
            _activateButton.onClick.AddListener(HandleActivateClicked);
        }

        if (_deactivateButton != null)
        {
            _deactivateButton.onClick.AddListener(HandleDeactivateClicked);
        }
    }

    private void ValidateReferences()
    {
        if (_dataSource == null)
        {
            Debug.LogError("[RuneBookPresenter] 데이터 소스가 할당되지 않았습니다.", this);
        }

        if (_entrySlotContainer == null)
        {
            Debug.LogError("[RuneBookPresenter] 룬 목록 칸의 부모가 할당되지 않았습니다.", this);
        }

        if (_activatedSlotContainer == null)
        {
            Debug.LogError("[RuneBookPresenter] 활성 룬 칸의 부모가 할당되지 않았습니다.", this);
        }

        if (_detailView == null)
        {
            Debug.LogError("[RuneBookPresenter] 상세 View가 할당되지 않았습니다.", this);
        }

        if (_activateButton == null || _deactivateButton == null)
        {
            Debug.LogError("[RuneBookPresenter] 활성화/비활성화 버튼이 할당되지 않았습니다.", this);
        }
    }

    private static RuneSymbolSlotView[] CollectSlots(Transform container, Action<RuneSymbolSlotView> onClicked)
    {
        if (container == null)
        {
            return Array.Empty<RuneSymbolSlotView>();
        }

        List<RuneSymbolSlotView> slots = new();
        foreach (Transform child in container)
        {
            if (child.TryGetComponent(out RuneSymbolSlotView slot))
            {
                slot.SetIndex(slots.Count);
                slot.Clicked += onClicked;
                slots.Add(slot);
            }
        }

        return slots.ToArray();
    }

    private void Subscribe(RuneBookDataSource dataSource)
    {
        if (_subscribedDataSource == dataSource)
        {
            return;
        }

        Unsubscribe();

        if (dataSource != null)
        {
            dataSource.Changed += Refresh;
            _subscribedDataSource = dataSource;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribedDataSource != null)
        {
            _subscribedDataSource.Changed -= Refresh;
            _subscribedDataSource = null;
        }
    }

    private void Refresh()
    {
        if (_dataSource == null)
        {
            return;
        }

        _entries = _dataSource.GetEntries() ?? Array.Empty<RuneBookEntryData>();
        ClampSelection();

        RefreshEntrySlots();
        RefreshActivatedSlots();
        RefreshDetail();
        RefreshButtons();

        if (_ownedRuneStoneList != null)
        {
            _ownedRuneStoneList.Show(_dataSource.GetOwnedRuneStones());
        }
    }

    private void ClampSelection()
    {
        if (_selectedEntryIndex >= _entries.Count ||
            (_selectedEntryIndex != NoSelection && !_entries[_selectedEntryIndex].IsRevealed))
        {
            _selectedEntryIndex = NoSelection;
        }

        if (_selectedSlotIndex >= _dataSource.ActiveSlotCount)
        {
            _selectedSlotIndex = 0;
        }
    }

    private void RefreshEntrySlots()
    {
        if (_entries.Count > _entrySlots.Length && !_hasWarnedSlotShortage)
        {
            _hasWarnedSlotShortage = true;
            Debug.LogWarning(
                $"[RuneBookPresenter] 룬 효과는 {_entries.Count}개인데 목록 칸은 {_entrySlots.Length}개입니다. 남는 룬은 표시되지 않습니다.",
                this);
        }

        for (int i = 0; i < _entrySlots.Length; i++)
        {
            RuneSymbolSlotView slot = _entrySlots[i];
            bool hasEntry = i < _entries.Count;
            slot.gameObject.SetActive(hasEntry);
            if (!hasEntry)
            {
                continue;
            }

            RuneBookEntryData entry = _entries[i];
            slot.Show(ToSlotState(entry), GetVisibleIcon(entry));
            slot.SetSelected(i == _selectedEntryIndex);
        }
    }

    private void RefreshActivatedSlots()
    {
        for (int i = 0; i < _activatedSlots.Length; i++)
        {
            RuneSymbolSlotView slot = _activatedSlots[i];
            bool isAvailableSlot = i < _dataSource.MaxSlotCount;
            slot.gameObject.SetActive(isAvailableSlot);
            if (!isAvailableSlot)
            {
                continue;
            }

            if (i >= _dataSource.ActiveSlotCount)
            {
                slot.Show(RuneSymbolSlotState.Locked, null);
                slot.SetSelected(false);
                continue;
            }

            RuneBookEntryData equippedEntry = FindEntry(_dataSource.GetEquippedEntryId(i));
            if (equippedEntry == null)
            {
                slot.Show(RuneSymbolSlotState.Empty, null);
            }
            else
            {
                slot.Show(ToSlotState(equippedEntry), GetVisibleIcon(equippedEntry));
            }

            slot.SetSelected(i == _selectedSlotIndex);
        }
    }

    private void RefreshDetail()
    {
        if (_detailView == null)
        {
            return;
        }

        RuneBookEntryData entry = GetSelectedEntry() ?? FindEntry(_dataSource.GetEquippedEntryId(_selectedSlotIndex));
        _detailView.Show(entry);
    }

    private void RefreshButtons()
    {
        RuneBookEntryData selectedEntry = GetSelectedEntry();

        if (_activateButton != null)
        {
            _activateButton.interactable =
                selectedEntry != null && _dataSource.CanEquip(_selectedSlotIndex, selectedEntry.Id);
        }

        if (_deactivateButton != null)
        {
            _deactivateButton.interactable = _dataSource.CanUnequip(_selectedSlotIndex);
        }
    }

    private void HandleEntrySlotClicked(RuneSymbolSlotView slot)
    {
        if (slot.Index >= _entries.Count || !_entries[slot.Index].IsRevealed)
        {
            return;
        }

        _selectedEntryIndex = slot.Index;
        Refresh();
    }

    private void HandleActivatedSlotClicked(RuneSymbolSlotView slot)
    {
        if (_dataSource == null || slot.Index >= _dataSource.ActiveSlotCount)
        {
            return;
        }

        _selectedSlotIndex = slot.Index;

        // 장착된 룬이 있으면 목록에서도 선택해 상세를 바로 보여준다.
        int equippedEntryIndex = FindEntryIndex(_dataSource.GetEquippedEntryId(slot.Index));
        if (equippedEntryIndex != NoSelection)
        {
            _selectedEntryIndex = equippedEntryIndex;
        }

        Refresh();
    }

    private void HandleActivateClicked()
    {
        RuneBookEntryData selectedEntry = GetSelectedEntry();
        if (_dataSource == null || selectedEntry == null)
        {
            return;
        }

        if (!_dataSource.TryEquip(_selectedSlotIndex, selectedEntry.Id))
        {
            Debug.Log($"[RuneBookPresenter] {selectedEntry.EnglishName}을(를) {_selectedSlotIndex + 1}번 칸에 활성화하지 못했습니다.");
            Refresh();
        }
    }

    private void HandleDeactivateClicked()
    {
        if (_dataSource == null)
        {
            return;
        }

        if (!_dataSource.TryUnequip(_selectedSlotIndex))
        {
            Debug.Log($"[RuneBookPresenter] {_selectedSlotIndex + 1}번 칸을 비활성화하지 못했습니다.");
            Refresh();
        }
    }

    private RuneBookEntryData GetSelectedEntry()
    {
        return _selectedEntryIndex == NoSelection ? null : _entries[_selectedEntryIndex];
    }

    private RuneBookEntryData FindEntry(string entryId)
    {
        int index = FindEntryIndex(entryId);
        return index == NoSelection ? null : _entries[index];
    }

    private int FindEntryIndex(string entryId)
    {
        if (string.IsNullOrEmpty(entryId))
        {
            return NoSelection;
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Id == entryId)
            {
                return i;
            }
        }

        return NoSelection;
    }

    private static RuneSymbolSlotState ToSlotState(RuneBookEntryData entry)
    {
        return entry.RevealLevel switch
        {
            RuneWordRevealLevel.Hidden => RuneSymbolSlotState.Locked,
            RuneWordRevealLevel.Listed => RuneSymbolSlotState.Listed,
            _ => RuneSymbolSlotState.Detailed
        };
    }

    private static Sprite GetVisibleIcon(RuneBookEntryData entry)
    {
        // 공개 단계와 상관없이 룬마다 자기 아이콘을 보여준다. (잠김/이름 공개 단계는 칸 색으로 어둡게 표시)
        return entry.Icon;
    }
}
