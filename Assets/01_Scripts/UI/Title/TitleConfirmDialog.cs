using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 타이틀 화면의 확인 창 (새 게임을 시작할까요? 등). 화면 가운데 장식선 띠에 질문과 두 선택지를 띄운다.
/// 선택은 창이 직접 들고 있다. (마우스로 다른 곳을 눌러 EventSystem 선택이 풀려도 키보드가 그대로 먹힌다)
/// - ← → / A D (↑ ↓ / W S도 같다) : 선택지 이동
/// - F / Space / Enter : 선택한 쪽으로 결정
/// - Esc : 취소
/// - 마우스를 올리면 선택, 누르면 결정
/// 실수로 누르지 않도록 열릴 때 취소가 먼저 선택되고, 열린 직후 잠깐은 입력을 받지 않는다.
/// 실제 시간으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
public class TitleConfirmDialog : MonoBehaviour
{
    private const int CancelIndex = 0;
    private const int ConfirmIndex = 1;

    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _bodyText;
    [Tooltip("왼쪽 선택지 (취소)")]
    [SerializeField] private RuneMenuItemView _cancelItem;
    [Tooltip("오른쪽 선택지 (확인)")]
    [SerializeField] private RuneMenuItemView _confirmItem;
    [Tooltip("확인 선택지 글자")]
    [SerializeField] private TMP_Text _confirmLabel;
    [Tooltip("창 전체 (나타날 때 서서히 밝아진다)")]
    [SerializeField] private CanvasGroup _group;

    [SerializeField, Min(0.01f)] private float _fadeDuration = 0.2f;
    [Tooltip("열린 뒤 이 시간 동안은 입력을 받지 않는다 (창을 연 키가 바로 먹히지 않게)")]
    [SerializeField, Min(0f)] private float _inputDelay = 0.15f;

    private Action _onConfirm;
    private Action _onCancel;
    private int _selected;
    private float _openTime;
    private bool _isInitialized;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_cancelItem != null)
        {
            _cancelItem.Hovered -= HandleHovered;
            _cancelItem.Clicked -= HandleClicked;
        }

        if (_confirmItem != null)
        {
            _confirmItem.Hovered -= HandleHovered;
            _confirmItem.Clicked -= HandleClicked;
        }
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_cancelItem != null)
        {
            _cancelItem.Hovered += HandleHovered;
            _cancelItem.Clicked += HandleClicked;
        }

        if (_confirmItem != null)
        {
            _confirmItem.Hovered += HandleHovered;
            _confirmItem.Clicked += HandleClicked;
        }
    }

    public void Show(string title, string body, string confirmLabel, Action onConfirm, Action onCancel)
    {
        Initialize();
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (_titleText != null) _titleText.text = title;
        if (_bodyText != null) _bodyText.text = body;
        if (_confirmLabel != null) _confirmLabel.text = confirmLabel;

        gameObject.SetActive(true);
        _openTime = Time.unscaledTime;
        if (_group != null)
        {
            _group.alpha = 0f;
        }

        Select(CancelIndex, true);
    }

    private void Update()
    {
        if (_group != null && _group.alpha < 1f)
        {
            _group.alpha = Mathf.Clamp01((Time.unscaledTime - _openTime) / _fadeDuration);
        }

        if (Time.unscaledTime - _openTime < _inputDelay)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)
            || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            Move(CancelIndex);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)
                 || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            Move(ConfirmIndex);
        }
        else if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space)
                 || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Submit();
        }
    }

    private void HandleHovered(RuneMenuItemView item)
    {
        if (IsReady())
        {
            Move(item == _confirmItem ? ConfirmIndex : CancelIndex);
        }
    }

    private void HandleClicked(RuneMenuItemView item)
    {
        if (!IsReady())
        {
            return;
        }

        Select(item == _confirmItem ? ConfirmIndex : CancelIndex, false);
        Submit();
    }

    private bool IsReady()
    {
        return IsOpen && Time.unscaledTime - _openTime >= _inputDelay;
    }

    private void Move(int index)
    {
        if (index == _selected)
        {
            return;
        }

        Select(index, false);
        Sfx.Play(Sfx.MenuMove);
    }

    private void Select(int index, bool snap)
    {
        _selected = index;
        if (_cancelItem != null)
        {
            _cancelItem.IsSelected = index == CancelIndex;
            if (snap) _cancelItem.SnapSelection();
        }

        if (_confirmItem != null)
        {
            _confirmItem.IsSelected = index == ConfirmIndex;
            if (snap) _confirmItem.SnapSelection();
        }
    }

    private void Submit()
    {
        Sfx.Play(Sfx.MenuConfirm);
        if (_selected == ConfirmIndex)
        {
            Confirm();
        }
        else
        {
            Cancel();
        }
    }

    public void Confirm()
    {
        if (!IsOpen)
        {
            return;
        }

        Action callback = _onConfirm;
        Hide();
        callback?.Invoke();
    }

    public void Cancel()
    {
        if (!IsOpen)
        {
            return;
        }

        Action callback = _onCancel;
        Hide();
        callback?.Invoke();
    }

    public void Hide()
    {
        _onConfirm = null;
        _onCancel = null;
        gameObject.SetActive(false);
    }
}
