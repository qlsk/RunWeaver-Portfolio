using System;
using UnityEngine;

/// <summary>
/// 엔딩 메뉴. 항목 중 하나가 항상 선택되어 있다.
/// - 위/아래 방향키(W/S)로 고르고 Enter(Space)로 실행한다.
/// - 마우스를 올리면 선택되고, 누르면 실행한다.
/// - 항목마다 바로 실행하는 단축키를 둘 수 있다. (예: R 다시 시작, Esc 타이틀)
/// 한 번 실행하면 다시 열릴 때까지 입력을 받지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class EndingMenuView : MonoBehaviour
{
    [SerializeField] private EndingMenuItemView[] _items;
    [Tooltip("항목별 단축키. 순서는 _items와 같다. 없으면 None")]
    [SerializeField] private KeyCode[] _shortcutKeys = { KeyCode.R, KeyCode.Escape };
    [SerializeField, Min(0)] private int _defaultIndex;
    [Tooltip("열린 직후 이 시간 동안은 입력을 받지 않는다 (판이 끝날 때 누르던 키가 바로 먹히지 않게)")]
    [SerializeField, Min(0f)] private float _inputDelay = 0.6f;

    private int _selectedIndex;
    private bool _isLocked;
    private float _openedTime;

    /// <summary>항목을 실행했을 때. 인자는 항목 순서</summary>
    public event Action<int> Confirmed;

    private void OnEnable()
    {
        _isLocked = false;
        _openedTime = Time.unscaledTime;

        foreach (EndingMenuItemView item in _items)
        {
            if (item != null)
            {
                item.Hovered += HandleHovered;
                item.Clicked += HandleClicked;
            }
        }

        Select(Mathf.Clamp(_defaultIndex, 0, Mathf.Max(0, _items.Length - 1)), true);
    }

    private void OnDisable()
    {
        foreach (EndingMenuItemView item in _items)
        {
            if (item != null)
            {
                item.Hovered -= HandleHovered;
                item.Clicked -= HandleClicked;
            }
        }
    }

    private void Update()
    {
        if (!CanAcceptInput())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            Select((_selectedIndex - 1 + _items.Length) % _items.Length, false);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            Select((_selectedIndex + 1) % _items.Length, false);
        }
        else if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return) ||
                 Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            Confirm(_selectedIndex);
            return;
        }

        for (int i = 0; i < _shortcutKeys.Length && i < _items.Length; i++)
        {
            if (_shortcutKeys[i] != KeyCode.None && Input.GetKeyDown(_shortcutKeys[i]))
            {
                Select(i, false);
                Confirm(i);
                return;
            }
        }
    }

    private bool CanAcceptInput()
    {
        return !_isLocked && _items.Length > 0 && Time.unscaledTime - _openedTime >= _inputDelay;
    }

    private void Select(int index, bool snap)
    {
        _selectedIndex = index;
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                continue;
            }

            _items[i].IsSelected = i == index;
            if (snap)
            {
                _items[i].SnapSelection();
            }
        }
    }

    private void Confirm(int index)
    {
        if (_isLocked)
        {
            return;
        }

        _isLocked = true;
        if (index >= 0 && index < _items.Length && _items[index] != null)
        {
            _items[index].PlayConfirmFlash();
        }

        Confirmed?.Invoke(index);
    }

    private void HandleHovered(EndingMenuItemView item)
    {
        if (_isLocked)
        {
            return;
        }

        int index = Array.IndexOf(_items, item);
        if (index >= 0 && index != _selectedIndex)
        {
            Select(index, false);
        }
    }

    private void HandleClicked(EndingMenuItemView item)
    {
        if (!CanAcceptInput())
        {
            return;
        }

        int index = Array.IndexOf(_items, item);
        if (index >= 0)
        {
            Select(index, false);
            Confirm(index);
        }
    }
}
