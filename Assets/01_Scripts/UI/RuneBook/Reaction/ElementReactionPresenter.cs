using System;
using UnityEngine;

/// <summary>
/// 원소 반응 도감 페이지. 조합표 칸을 누르면 오른쪽 페이지에 설명을 보여준다.
/// 처음 열면 첫 반응 칸(Fire + Fire)을 선택한다. 모든 반응은 처음부터 공개한다.
/// 데이터 소스를 비워 두면 자식이나 씬에서 찾는다.
/// </summary>
[DisallowMultipleComponent]
public class ElementReactionPresenter : MonoBehaviour
{
    [Serializable]
    private class ElementIcon
    {
        public ReactionElement element;
        public Sprite icon;
    }

    [SerializeField] private ElementReactionDataSource _dataSource;
    [SerializeField] private ElementReactionCellView[] _cells;
    [SerializeField] private ElementReactionDetailView _detailView;
    [SerializeField] private ElementIcon[] _elementIcons;

    private ElementReactionCellView _selected;
    private bool _isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        RefreshCells();
        if (_selected == null)
        {
            Select(FindFirstReactionCell());
        }
        else
        {
            Select(_selected);
        }
    }

    private void OnDestroy()
    {
        foreach (ElementReactionCellView cell in _cells)
        {
            if (cell != null)
            {
                cell.Clicked -= Select;
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
            _dataSource = GetComponentInChildren<ElementReactionDataSource>(true);
        }

        if (_dataSource == null)
        {
            _dataSource = FindAnyObjectByType<ElementReactionDataSource>();
        }

        if (_dataSource == null)
        {
            Debug.LogError("[ElementReactionPresenter] ElementReactionDataSource를 찾을 수 없습니다.", this);
        }

        foreach (ElementReactionCellView cell in _cells)
        {
            if (cell != null)
            {
                cell.Clicked += Select;
            }
        }
    }

    private void RefreshCells()
    {
        foreach (ElementReactionCellView cell in _cells)
        {
            if (cell != null)
            {
                cell.Show(GetData(cell), GetIcon(cell.ElementA));
            }
        }
    }

    private void Select(ElementReactionCellView cell)
    {
        if (cell == null)
        {
            return;
        }

        _selected = cell;
        foreach (ElementReactionCellView other in _cells)
        {
            if (other != null)
            {
                other.SetSelected(other == cell);
            }
        }

        if (_detailView != null && _dataSource != null)
        {
            _detailView.Show(GetData(cell), GetIcon, _dataSource.GetBaseEffect);
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

    private ElementReactionCellView FindFirstReactionCell()
    {
        foreach (ElementReactionCellView cell in _cells)
        {
            if (cell != null && !cell.IsBaseElement)
            {
                return cell;
            }
        }

        return _cells.Length > 0 ? _cells[0] : null;
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
