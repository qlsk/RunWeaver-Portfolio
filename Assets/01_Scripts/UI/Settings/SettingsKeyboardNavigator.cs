using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 설정 창을 키보드로 다룬다. 창이 켜져 있는 동안 EventSystem의 기본 방향 이동은 끄고 이 스크립트가 맡는다.
/// - W / S / ↑ / ↓ : 위아래 줄로 이동 (끝에서 반대쪽으로 넘어간다)
/// - A / D / ← / → : 슬라이더는 값을 바꾸고(누르고 있으면 계속), 스위치는 켜고 끄고, 버튼 줄에서는 옆 버튼으로 이동
/// - Space / Enter : 스위치는 켜고 끄고, 버튼은 누른다
/// 일시정지 중에도 동작하도록 실제 시간을 쓴다.
/// </summary>
[DisallowMultipleComponent]
public class SettingsKeyboardNavigator : MonoBehaviour
{
    [Serializable]
    private class Row
    {
        [Tooltip("한 줄에 있는 선택 항목. 버튼 줄처럼 여러 개면 왼쪽부터")]
        public Selectable[] items;
    }

    [SerializeField] private Row[] _rows;

    [Header("슬라이더")]
    [SerializeField, Range(0.01f, 0.5f)] private float _sliderStep = 0.05f;
    [Tooltip("누르고 있을 때 반복이 시작되기까지 시간")]
    [SerializeField, Min(0f)] private float _repeatDelay = 0.35f;
    [Tooltip("반복 간격")]
    [SerializeField, Min(0.01f)] private float _repeatInterval = 0.06f;

    private bool _savedSendNavigation = true;
    private EventSystem _system;
    private int _heldDirection;
    private float _nextRepeatTime;

    private void OnEnable()
    {
        _system = EventSystem.current;
        if (_system != null)
        {
            _savedSendNavigation = _system.sendNavigationEvents;
            _system.sendNavigationEvents = false;
        }

        _heldDirection = 0;
    }

    private void OnDisable()
    {
        if (_system != null)
        {
            _system.sendNavigationEvents = _savedSendNavigation;
        }
    }

    private void Update()
    {
        if (_rows == null || _rows.Length == 0 || _system == null)
        {
            return;
        }

        FindCurrent(out int row, out int column);
        if (row < 0)
        {
            // 선택된 것이 없으면 아무 키나 눌렀을 때 첫 항목을 고른다.
            if (AnyNavigationKeyDown())
            {
                Select(0, 0);
            }

            return;
        }

        if (Pressed(KeyCode.W, KeyCode.UpArrow))
        {
            MoveRow(row, column, -1);
            return;
        }

        if (Pressed(KeyCode.S, KeyCode.DownArrow))
        {
            MoveRow(row, column, 1);
            return;
        }

        Selectable current = _rows[row].items[column];
        int horizontal = Held(KeyCode.D, KeyCode.RightArrow) ? 1 : Held(KeyCode.A, KeyCode.LeftArrow) ? -1 : 0;
        bool horizontalDown = Pressed(KeyCode.D, KeyCode.RightArrow) || Pressed(KeyCode.A, KeyCode.LeftArrow);

        if (current is Slider slider)
        {
            HandleSlider(slider, horizontal, horizontalDown);
        }
        else if (horizontalDown && horizontal != 0)
        {
            if (current is Toggle toggle)
            {
                toggle.isOn = !toggle.isOn;
            }
            else
            {
                int count = _rows[row].items.Length;
                int next = Mathf.Clamp(column + horizontal, 0, count - 1);
                if (next != column)
                {
                    Select(row, next);
                }
            }
        }

        if (Pressed(KeyCode.Space, KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Submit(current);
        }
    }

    private void HandleSlider(Slider slider, int direction, bool justPressed)
    {
        if (direction == 0)
        {
            _heldDirection = 0;
            return;
        }

        float now = Time.unscaledTime;
        if (justPressed || direction != _heldDirection)
        {
            _heldDirection = direction;
            _nextRepeatTime = now + _repeatDelay;
            Step(slider, direction);
        }
        else if (now >= _nextRepeatTime)
        {
            _nextRepeatTime = now + _repeatInterval;
            Step(slider, direction);
        }
    }

    private void Step(Slider slider, int direction)
    {
        float range = slider.maxValue - slider.minValue;
        float step = slider.wholeNumbers ? 1f : range * _sliderStep;
        slider.value = Mathf.Clamp(slider.value + step * direction, slider.minValue, slider.maxValue);
    }

    private static void Submit(Selectable current)
    {
        switch (current)
        {
            case Toggle toggle:
                toggle.isOn = !toggle.isOn;
                break;
            case Button button:
                if (button.interactable)
                {
                    button.onClick.Invoke();
                }

                break;
        }
    }

    private void MoveRow(int row, int column, int step)
    {
        int count = _rows.Length;
        for (int k = 1; k <= count; k++)
        {
            int next = ((row + step * k) % count + count) % count;
            Selectable[] items = _rows[next].items;
            if (items != null && items.Length > 0)
            {
                Select(next, Mathf.Min(column, items.Length - 1));
                return;
            }
        }
    }

    private void Select(int row, int column)
    {
        Selectable target = _rows[row].items[column];
        if (target != null)
        {
            _system.SetSelectedGameObject(target.gameObject);
        }

        _heldDirection = 0;
    }

    private void FindCurrent(out int row, out int column)
    {
        row = column = -1;
        GameObject selected = _system.currentSelectedGameObject;
        if (selected == null)
        {
            return;
        }

        for (int r = 0; r < _rows.Length; r++)
        {
            Selectable[] items = _rows[r].items;
            if (items == null)
            {
                continue;
            }

            for (int c = 0; c < items.Length; c++)
            {
                if (items[c] != null && items[c].gameObject == selected)
                {
                    row = r;
                    column = c;
                    return;
                }
            }
        }
    }

    private static bool Pressed(KeyCode a, KeyCode b)
    {
        return Input.GetKeyDown(a) || Input.GetKeyDown(b);
    }

    private static bool Held(KeyCode a, KeyCode b)
    {
        return Input.GetKey(a) || Input.GetKey(b);
    }

    private static bool AnyNavigationKeyDown()
    {
        return Pressed(KeyCode.W, KeyCode.UpArrow) || Pressed(KeyCode.S, KeyCode.DownArrow) ||
               Pressed(KeyCode.A, KeyCode.LeftArrow) || Pressed(KeyCode.D, KeyCode.RightArrow) ||
               Pressed(KeyCode.Space, KeyCode.Return);
    }
}
