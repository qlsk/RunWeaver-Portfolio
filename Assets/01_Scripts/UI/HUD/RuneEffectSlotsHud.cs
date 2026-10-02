using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD의 활성 룬 효과 슬롯. 룬북과 같은 데이터 소스를 읽어 장착된 룬을 표시한다.
/// 장착된 칸: 아이콘 표시 / 빈 칸: 아이콘 숨김 / 아직 열리지 않은 칸: 배경을 어둡게 표시
/// </summary>
[DisallowMultipleComponent]
public class RuneEffectSlotsHud : MonoBehaviour
{
    [SerializeField] private RuneBookDataSource _dataSource;

    [Header("슬롯 (왼쪽부터 순서대로)")]
    [SerializeField] private Image[] _slotBackgrounds;
    [SerializeField] private Image[] _slotIcons;

    [Header("색상")]
    [SerializeField] private Color _openBackgroundColor = Color.white;
    [SerializeField] private Color _lockedBackgroundColor = new(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color _iconColor = Color.white;

    private Sprite[] _defaultIcons;
    private RuneBookDataSource _subscribedDataSource;

    private void Awake()
    {
        int iconCount = _slotIcons?.Length ?? 0;
        _defaultIcons = new Sprite[iconCount];
        for (int i = 0; i < iconCount; i++)
        {
            _defaultIcons[i] = _slotIcons[i] != null ? _slotIcons[i].sprite : null;
        }

        if (_dataSource == null)
        {
            Debug.LogWarning("[RuneEffectSlotsHud] 룬 데이터 소스가 할당되지 않아 룬 슬롯을 갱신하지 않습니다.", this);
        }
    }

    private void OnEnable()
    {
        if (_dataSource != null)
        {
            _dataSource.Changed += Refresh;
            _subscribedDataSource = _dataSource;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (_subscribedDataSource != null)
        {
            _subscribedDataSource.Changed -= Refresh;
            _subscribedDataSource = null;
        }
    }

    private void Start()
    {
        // 데이터 소스가 Awake에서 데이터를 준비하므로 첫 화면을 한 번 더 갱신한다.
        Refresh();
    }

    private void Refresh()
    {
        if (_dataSource == null || _slotBackgrounds == null)
        {
            return;
        }

        IReadOnlyList<RuneBookEntryData> entries = _dataSource.GetEntries();

        for (int i = 0; i < _slotBackgrounds.Length; i++)
        {
            bool isOpen = i < _dataSource.ActiveSlotCount;
            if (_slotBackgrounds[i] != null)
            {
                _slotBackgrounds[i].color = isOpen ? _openBackgroundColor : _lockedBackgroundColor;
            }

            RuneBookEntryData equippedEntry = isOpen ? FindEntry(entries, _dataSource.GetEquippedEntryId(i)) : null;
            ShowIcon(i, equippedEntry);
        }
    }

    private void ShowIcon(int index, RuneBookEntryData entry)
    {
        if (_slotIcons == null || index >= _slotIcons.Length || _slotIcons[index] == null)
        {
            return;
        }

        Image icon = _slotIcons[index];
        icon.enabled = entry != null;
        if (entry == null)
        {
            return;
        }

        icon.sprite = entry.Icon != null ? entry.Icon : _defaultIcons[index];
        icon.color = _iconColor;
    }

    private static RuneBookEntryData FindEntry(IReadOnlyList<RuneBookEntryData> entries, string entryId)
    {
        if (entries == null || string.IsNullOrEmpty(entryId))
        {
            return null;
        }

        foreach (RuneBookEntryData entry in entries)
        {
            if (entry.Id == entryId)
            {
                return entry;
            }
        }

        return null;
    }
}
