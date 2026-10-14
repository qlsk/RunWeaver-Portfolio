using System;
using UnityEngine;

/// <summary>
/// 타이틀 글자 메뉴. 항목 하나가 항상 선택되어 있다.
/// - 위/아래 방향키(W/S)로 고르고 F(Enter, Space)로 결정한다. 누를 수 없는 항목은 건너뛴다. (전투 씬의 선택 창들과 같은 키)
/// - 마우스를 올리면 선택되고, 누르면 결정한다.
/// - PlayAppear로 항목이 차례로 떠오른다. 그 전에는 입력을 받지 않는다.
/// - 결정하면 잠긴다. 다시 받으려면 SetLocked(false). (설정 창을 닫았을 때 등)
/// </summary>
[DisallowMultipleComponent]
public class TitleMenuView : MonoBehaviour
{
    [SerializeField] private RuneMenuItemView[] _items;
    [SerializeField, Min(0)] private int _defaultIndex;

    [Header("등장")]
    [SerializeField, Min(0f)] private float _appearInterval = 0.13f;
    [SerializeField, Min(0.01f)] private float _appearDuration = 0.5f;
    [Tooltip("메뉴 아래 조작 안내. 항목이 다 나온 뒤 나타난다")]
    [SerializeField] private CanvasGroup _hint;
    [SerializeField, Min(0.01f)] private float _hintFadeDuration = 0.6f;

    [Tooltip("잠금을 풀고 이 시간 동안은 입력을 받지 않는다 (창을 닫은 키가 바로 먹히지 않게)")]
    [SerializeField, Min(0f)] private float _unlockDelay = 0.15f;

    private int _selectedIndex;
    private bool _isLocked = true;
    private bool _hasAppeared;
    private float _appearStart = -1f;
    private float _unlockTime;
    private int _pressIndex = -1;
    private float _press;

    /// <summary>항목을 결정했을 때. 인자는 항목 순서</summary>
    public event Action<int> Confirmed;
    public event Action Moved;

    public bool IsLocked => _isLocked;

    /// <summary>지금 선택된 항목 순서</summary>
    public int SelectedIndex => _selectedIndex;

    /// <summary>항목. 없으면 null</summary>
    public RuneMenuItemView GetItem(int index)
    {
        return index >= 0 && index < _items.Length ? _items[index] : null;
    }

    /// <summary>항목을 바로 선택한다. 누를 수 없는 항목이면 무시한다. (이어할 판이 있으면 이어하기를 먼저 선택)</summary>
    public void SelectItem(int index)
    {
        if (index >= 0 && index < _items.Length && _items[index] != null && !_items[index].IsDisabled)
        {
            Select(index, true);
        }
    }

    private void OnEnable()
    {
        foreach (RuneMenuItemView item in _items)
        {
            if (item != null)
            {
                item.Hovered += HandleHovered;
                item.Clicked += HandleClicked;
                if (!_hasAppeared)
                {
                    item.Appear = 0f;
                }
            }
        }

        if (_hint != null && !_hasAppeared)
        {
            _hint.alpha = 0f;
        }

        Select(FindSelectable(Mathf.Clamp(_defaultIndex, 0, Mathf.Max(0, _items.Length - 1)), 1), true);
    }

    private void OnDisable()
    {
        foreach (RuneMenuItemView item in _items)
        {
            if (item != null)
            {
                item.Hovered -= HandleHovered;
                item.Clicked -= HandleClicked;
            }
        }
    }

    /// <summary>항목을 차례로 띄우고 입력을 받기 시작한다.</summary>
    public void PlayAppear()
    {
        _appearStart = Time.unscaledTime;
        _hasAppeared = true;
        SetLocked(false);
    }

    public void SetLocked(bool isLocked)
    {
        _isLocked = isLocked;
        if (!isLocked)
        {
            _unlockTime = Time.unscaledTime;
        }
    }

    /// <summary>결정 연출. 고른 항목은 빛나고 나머지는 사라진다.</summary>
    public void SetPress(int index, float press)
    {
        _pressIndex = index;
        _press = press;
    }

    private void Update()
    {
        UpdateAppear();

        if (_isLocked || !_hasAppeared || Time.unscaledTime - _unlockTime < _unlockDelay || _items.Length == 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            Select(FindSelectable(_selectedIndex - 1, -1), false);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            Select(FindSelectable(_selectedIndex + 1, 1), false);
        }
        else if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return) ||
                 Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            Confirm(_selectedIndex);
        }
    }

    private void UpdateAppear()
    {
        float now = Time.unscaledTime;
        for (int i = 0; i < _items.Length; i++)
        {
            RuneMenuItemView item = _items[i];
            if (item == null)
            {
                continue;
            }

            float appear = _appearStart < 0f ? 0f : EaseOut((now - _appearStart - i * _appearInterval) / _appearDuration);
            if (_pressIndex >= 0)
            {
                item.Press = i == _pressIndex ? _press : 0f;
                if (i != _pressIndex)
                {
                    appear *= Mathf.Clamp01(1f - _press * 1.6f);
                }
            }

            item.Appear = appear;
        }

        if (_hint != null)
        {
            float start = _appearStart + _items.Length * _appearInterval + _appearDuration * 0.5f;
            float alpha = _appearStart < 0f ? 0f : EaseOut((now - start) / _hintFadeDuration);
            if (_pressIndex >= 0)
            {
                alpha *= Mathf.Clamp01(1f - _press * 2f);
            }

            _hint.alpha = alpha;
        }
    }

    private int FindSelectable(int start, int step)
    {
        int count = _items.Length;
        for (int k = 0; k < count; k++)
        {
            int index = ((start + step * k) % count + count) % count;
            if (_items[index] != null && !_items[index].IsDisabled)
            {
                return index;
            }
        }

        return Mathf.Clamp(start, 0, Mathf.Max(0, count - 1));
    }

    private void Select(int index, bool snap)
    {
        // 바뀌기 전의 선택 항목을 기억해 둔다.
        int prevIndex = _selectedIndex;

        // 새 선택 항목을 기록한다.
        _selectedIndex = index;

        // 모든 항목의 선택 표시를 갱신한다.
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                continue;
            }

            // 즉시 전환이 지정되었으면 표시를 곧바로 맞춘다.
            _items[i].IsSelected = i == index;
            if (snap)
            {
                _items[i].SnapSelection();
            }
        }

        // 즉시 전환이 아니고, 선택 항목이 이전과 다르다면 이동 알림을 보낸다.
        if (!snap && prevIndex != _selectedIndex)
            Moved?.Invoke();
    }

    private void Confirm(int index)
    {
        if (_isLocked || index < 0 || index >= _items.Length || _items[index] == null || _items[index].IsDisabled)
        {
            return;
        }

        _isLocked = true;
        Confirmed?.Invoke(index);
    }

    private void HandleHovered(RuneMenuItemView item)
    {
        if (_isLocked || !_hasAppeared || item.IsDisabled)
        {
            return;
        }

        int index = Array.IndexOf(_items, item);
        if (index >= 0 && index != _selectedIndex)
        {
            Select(index, false);
        }
    }

    private void HandleClicked(RuneMenuItemView item)
    {
        if (_isLocked || !_hasAppeared || Time.unscaledTime - _unlockTime < _unlockDelay)
        {
            return;
        }

        int index = Array.IndexOf(_items, item);
        if (index >= 0 && !item.IsDisabled)
        {
            Select(index, false);
            Confirm(index);
        }
    }

    private static float EaseOut(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - (1f - x) * (1f - x) * (1f - x);
    }
}