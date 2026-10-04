using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD의 활성 룬 효과 슬롯. 현재 룬 효과 로드아웃을 읽어 장착된 룬을 표시한다.
/// 장착된 칸: 아이콘 표시 / 빈 칸: 아이콘 숨김 / 아직 열리지 않은 칸: 배경을 어둡게 표시
/// </summary>
[DisallowMultipleComponent]
public class RuneEffectSlotsHud : MonoBehaviour
{
    [Header("슬롯 (왼쪽부터 순서대로)")]
    [SerializeField] private Image[] _slotBackgrounds;
    [SerializeField] private Image[] _slotIcons;

    [Header("색상")]
    [SerializeField] private Color _openBackgroundColor = Color.white;
    [SerializeField] private Color _lockedBackgroundColor = new(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color _iconColor = Color.white;

    private Sprite[] _defaultIcons;
    private RuneEffectLoadoutManager _manager;
    private RuneEffectLoadout _loadout;
    private RuneEffectDatabase _database;

    private void Awake()
    {
        int iconCount = _slotIcons?.Length ?? 0;
        _defaultIcons = new Sprite[iconCount];
        for (int i = 0; i < iconCount; i++)
        {
            _defaultIcons[i] = _slotIcons[i] != null ? _slotIcons[i].sprite : null;
        }
    }

    private void OnEnable()
    {
        BindManager();
    }

    private void OnDisable()
    {
        if (_manager != null)
        {
            _manager.LoadoutReplaced -= BindLoadout;
        }
        _manager = null;
        if (_loadout != null)
            _loadout.LoadoutChanged -= Refresh;
        _loadout = null;
    }

    private void Update()
    {
        if (_manager != RuneEffectLoadoutManager.Instance || _database != RuneEffectDatabase.Instance)
            BindManager();
    }

    private void BindManager()
    {
        if (_manager != null)
            _manager.LoadoutReplaced -= BindLoadout;

        _manager = RuneEffectLoadoutManager.Instance;
        _database = RuneEffectDatabase.Instance;
        if (_manager != null)
            _manager.LoadoutReplaced += BindLoadout;

        BindLoadout();
    }

    private void BindLoadout()
    {
        if (_loadout != null)
            _loadout.LoadoutChanged -= Refresh;

        _loadout = _manager != null ? _manager.RuneEffectLoadout : null;
        if (_loadout != null)
            _loadout.LoadoutChanged += Refresh;

        Refresh(_loadout?.EquippedEffectIds);
    }

    private void Refresh(IReadOnlyList<string> equippedEffectIds)
    {
        int slotCount = Mathf.Max(_slotBackgrounds?.Length ?? 0, _slotIcons?.Length ?? 0);
        for (int i = 0; i < slotCount; i++)
        {
            bool isOpen = _loadout != null && i < _loadout.ActiveSlotCount;
            if (_slotBackgrounds != null && i < _slotBackgrounds.Length && _slotBackgrounds[i] != null)
            {
                _slotBackgrounds[i].color = isOpen ? _openBackgroundColor : _lockedBackgroundColor;
            }

            RuneEffectDefinition effect = null;
            string effectId = isOpen ? _loadout.GetEquippedEffectId(i) : null;
            if (!string.IsNullOrEmpty(effectId) && _database != null)
                _database.TryGetById(effectId, out effect);
            ShowIcon(i, effect);
        }
    }

    private void ShowIcon(int index, RuneEffectDefinition entry)
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
}
