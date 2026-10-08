using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 새 룬북(RuneBookV2)의 Reactions 탭. 원소 반응 조합표 도감이다.
/// 칸에 마우스를 올리면 Rune Words 탭과 같은 모양의 팝업에 반응(또는 기본 원소 효과) 설명을 띄운다.
/// 도감 역할만 하며, 칸을 눌러도 장착(Activated)은 바뀌지 않는다.
/// 각 칸에는 ElementReactionCellView와 RuneBookHoverRelay가 함께 있어야 한다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookV2ReactionPresenter : MonoBehaviour
{
    [Serializable]
    private class ElementIcon
    {
        public ReactionElement element;
        public Sprite icon;
    }

    [SerializeField] private ElementReactionDataSource _dataSource;
    [SerializeField] private ElementReactionCellView[] _cells;
    [SerializeField] private ElementIcon[] _elementIcons;

    [Header("팝업")]
    [SerializeField] private ElementReactionDetailView _detailView;
    [SerializeField] private RuneBookTooltipLayout _tooltip;

    [Tooltip("장착 조합을 읽을 룬북. 비워두면 부모에서 찾는다")]
    [SerializeField] private RuneBookV2Presenter _runeBookPresenter;

    private bool _isInitialized;
    private string _equippedKey;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        foreach (ElementReactionCellView cell in _cells)
        {
            if (cell != null)
            {
                cell.Show(GetData(cell), GetIcon(cell.ElementA));
                cell.SetSelected(false);
            }
        }

        _equippedKey = null;
        RefreshEquipped();
        HideTooltip();
    }

    private void LateUpdate()
    {
        // 룬북 칸을 바꾸면 (Confirm 전이어도) 바로 강조를 옮긴다.
        RefreshEquipped();
    }

    /// <summary>
    /// 지금 장착한 원소 조합 칸을 강조한다.
    /// 주력 원소(1번 칸)와 보조 원소(2번 칸)가 모두 있으면 두 원소의 반응 칸, 주력 원소만 있으면 그 원소의 머리 칸.
    /// </summary>
    private void RefreshEquipped()
    {
        ReactionElement? main = null;
        ReactionElement? sub = null;
        IReadOnlyList<string> slotIds = GetSlotIds();
        if (slotIds != null)
        {
            if (slotIds.Count > RuneEffectLoadout.PrimaryElementSlotIndex)
            {
                main = ToElement(slotIds[RuneEffectLoadout.PrimaryElementSlotIndex]);
            }

            if (slotIds.Count > RuneEffectLoadout.ElementRuneEffectSlotIndex)
            {
                sub = ToElement(slotIds[RuneEffectLoadout.ElementRuneEffectSlotIndex]);
            }
        }

        string key = $"{main}|{sub}";
        if (key == _equippedKey)
        {
            return;
        }

        _equippedKey = key;
        foreach (ElementReactionCellView cell in _cells)
        {
            if (cell == null)
            {
                continue;
            }

            bool isEquipped;
            if (!main.HasValue)
            {
                isEquipped = false;
            }
            else if (!sub.HasValue)
            {
                isEquipped = cell.IsBaseElement && cell.ElementA == main.Value;
            }
            else
            {
                isEquipped = !cell.IsBaseElement &&
                             ((cell.ElementA == main.Value && cell.ElementB == sub.Value) ||
                              (cell.ElementA == sub.Value && cell.ElementB == main.Value));
            }

            cell.SetEquipped(isEquipped);
        }
    }

    private IReadOnlyList<string> GetSlotIds()
    {
        if (_runeBookPresenter == null)
        {
            _runeBookPresenter = GetComponentInParent<RuneBookV2Presenter>(true);
            if (_runeBookPresenter == null)
            {
                _runeBookPresenter = FindAnyObjectByType<RuneBookV2Presenter>(FindObjectsInactive.Include);
            }
        }

        if (_runeBookPresenter != null)
        {
            return _runeBookPresenter.SlotIds;
        }

        // 룬북 화면이 없으면 확정된 장착 상태를 쓴다.
        PlayerRuneManager manager = PlayerRuneManager.Instance;
        return manager != null && manager.IsInitialized ? manager.RuneEffectLoadout.EquippedEffectIds : null;
    }

    private static ReactionElement? ToElement(string effectId)
    {
        return effectId switch
        {
            RuneEffectIds.Fire => ReactionElement.Fire,
            RuneEffectIds.Water => ReactionElement.Water,
            RuneEffectIds.Earth => ReactionElement.Earth,
            RuneEffectIds.Wind => ReactionElement.Wind,
            _ => null
        };
    }

    private void OnDisable()
    {
        // 칸 표시(선택 테두리)는 다시 켜질 때 OnEnable에서 되돌린다.
        HideTooltip();
    }

    private void OnDestroy()
    {
        if (_cells == null)
        {
            return;
        }

        foreach (ElementReactionCellView cell in _cells)
        {
            if (cell != null && cell.TryGetComponent(out RuneBookHoverRelay relay))
            {
                relay.PointerEntered -= HandlePointerEntered;
                relay.PointerExited -= HandlePointerExited;
            }
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
            _dataSource = FindAnyObjectByType<ElementReactionDataSource>();
        }

        if (_dataSource == null)
        {
            // 씬과 프리팹 어디에도 데이터 소스가 없으면 반응 칸이 전부 ???로 보인다.
            // 코드에 담긴 기본 데이터(StaticElementReactionDataSource)를 이 오브젝트에 붙여 쓴다.
            _dataSource = gameObject.AddComponent<StaticElementReactionDataSource>();
        }

        if (_cells == null)
        {
            _cells = Array.Empty<ElementReactionCellView>();
            return;
        }

        foreach (ElementReactionCellView cell in _cells)
        {
            if (cell == null)
            {
                continue;
            }

            if (!cell.TryGetComponent(out RuneBookHoverRelay relay))
            {
                relay = cell.gameObject.AddComponent<RuneBookHoverRelay>();
            }

            relay.PointerEntered += HandlePointerEntered;
            relay.PointerExited += HandlePointerExited;
        }
    }

    private void HandlePointerEntered(RuneBookHoverRelay relay)
    {
        if (!relay.TryGetComponent(out ElementReactionCellView cell) || _dataSource == null)
        {
            return;
        }

        cell.SetSelected(true);
        if (_detailView != null)
        {
            _detailView.Show(GetData(cell), GetIcon, _dataSource.GetBaseEffect);
        }

        if (_tooltip != null)
        {
            _tooltip.Show(relay.RectTransform);
        }
    }

    private void HandlePointerExited(RuneBookHoverRelay relay)
    {
        if (relay.TryGetComponent(out ElementReactionCellView cell))
        {
            cell.SetSelected(false);
        }

        HideTooltip();
    }

    private void HideTooltip()
    {
        if (_tooltip != null)
        {
            _tooltip.Hide();
        }
    }

    private ElementReactionData GetData(ElementReactionCellView cell)
    {
        if (_dataSource == null)
        {
            return null;
        }

        return cell.IsBaseElement
            ? _dataSource.GetBaseEffect(cell.ElementA)
            : _dataSource.GetReaction(cell.ElementA, cell.ElementB);
    }

    private Sprite GetIcon(ReactionElement element)
    {
        if (_elementIcons == null)
        {
            return null;
        }

        foreach (ElementIcon entry in _elementIcons)
        {
            if (entry != null && entry.element == element)
            {
                return entry.icon;
            }
        }

        return null;
    }
}
