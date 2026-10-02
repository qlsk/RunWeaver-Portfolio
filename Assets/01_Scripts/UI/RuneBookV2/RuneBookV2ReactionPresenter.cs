using System;
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

    private bool _isInitialized;

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

        HideTooltip();
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
            Debug.LogError("[RuneBookV2ReactionPresenter] ElementReactionDataSource를 찾을 수 없습니다.", this);
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
