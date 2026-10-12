using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룬북 확정 확인 창. (프리팹 RuneBookConfirmDialog : 핀으로 꽂은 쪽지, 문장, 제목, 구분선, 질문, [취소] [확정] 잉크 글씨)
/// 그림과 배치는 프리팹에서 정하고, 이 스크립트는 열고 닫기와 입력, 고른 버튼 표시만 맡는다.
/// - 키 : F / Space / Enter = 고른 버튼 실행, Esc = 취소, A·D·←·→ = 취소/확정 고르기. 처음에는 확정이 골라져 있다.
///   마우스는 RuneBookConfirmOption이 받는다. (올리면 고르고, 누르면 실행)
/// - 고른 버튼 : 글씨가 짙어지고 살짝 커지며 밑줄이 보인다. 실행하면 글씨가 꾹 눌린 듯 잠깐 줄었다 돌아온다.
/// - 열 때 쪽지(_panel)가 살짝 아래에서 올라오고, 닫을 때 흐려진다. 게임이 멈춘 상태(룬북)라 실제 시간으로 움직인다.
/// - 닫힌 프레임(ClosedFrame)을 남겨, 같은 키 입력으로 룬북이 닫히거나 창이 다시 열리지 않게 한다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookConfirmDialog : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup _group;
    [Tooltip("올라오며 나타나는 쪽지")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private TMP_Text _confirmText;
    [SerializeField] private Graphic _confirmLine;
    [SerializeField] private TMP_Text _cancelText;
    [SerializeField] private Graphic _cancelLine;

    [Header("색")]
    [SerializeField] private Color _normalColor = new(0.43f, 0.33f, 0.227f, 1f);
    [SerializeField] private Color _selectedColor = new(0.29f, 0.204f, 0.133f, 1f);

    [Header("움직임")]
    [SerializeField, Min(0.01f)] private float _openDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float _closeDuration = 0.15f;
    [SerializeField] private float _riseDistance = 30f;
    [SerializeField] private float _selectedScale = 1.08f;

    private static RuneBookConfirmDialog s_openDialog;

    private Action _onConfirm;
    private Action _onCancel;
    private Vector2 _panelBasePosition;
    private bool _hasBase;
    private int _selected; // 0 : 확정, 1 : 취소
    private int _openedFrame;
    private bool _isOpen;
    private bool _isClosing;
    private float _press;
    private Coroutine _routine;

    /// <summary>확인 창이 열려 있는지</summary>
    public static bool IsOpen => s_openDialog != null && s_openDialog._isOpen;

    /// <summary>마지막으로 닫힌 프레임. 같은 프레임에 다른 키 처리를 막을 때 쓴다.</summary>
    public static int ClosedFrame { get; private set; } = -1;

    /// <summary>확인 창을 연다. 확정하면 onConfirm, 취소하면 onCancel을 부른다.</summary>
    public void Show(Action onConfirm, Action onCancel = null)
    {
        CacheBase();
        _onConfirm = onConfirm;
        _onCancel = onCancel;
        _selected = 0;
        _press = 0f;
        _isOpen = true;
        _isClosing = false;
        _openedFrame = Time.frameCount;
        s_openDialog = this;
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        ApplySelection();
        RestartRoutine(OpenRoutine());
    }

    /// <summary>열려 있으면 결정 없이 바로 닫는다. (룬북이 닫힐 때)</summary>
    public void Hide()
    {
        if (!_isOpen && !_isClosing)
        {
            gameObject.SetActive(false);
            return;
        }

        StopRoutine();
        _onConfirm = null;
        _onCancel = null;
        MarkClosed();
        _isClosing = false;
        gameObject.SetActive(false);
    }

    /// <summary>고른 버튼을 바꾼다. (0 : 확정, 1 : 취소)</summary>
    public void Select(int index)
    {
        if (!_isOpen || _isClosing || _selected == index)
        {
            return;
        }

        _selected = index;
        ApplySelection();
    }

    /// <summary>버튼을 실행한다. (0 : 확정, 1 : 취소)</summary>
    public void Submit(int index)
    {
        if (!_isOpen || _isClosing)
        {
            return;
        }

        _selected = index;
        ApplySelection();
        RestartRoutine(CloseRoutine(index == 0));
    }

    private void CacheBase()
    {
        if (_hasBase || _panel == null)
        {
            return;
        }

        _hasBase = true;
        _panelBasePosition = _panel.anchoredPosition;
    }

    private IEnumerator OpenRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _openDuration);
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            SetAlpha(eased);
            SetPanelOffset(-_riseDistance * (1f - eased));
            yield return null;
        }

        SetAlpha(1f);
        SetPanelOffset(0f);
        _routine = null;
    }

    private IEnumerator CloseRoutine(bool confirmed)
    {
        _isClosing = true;
        _press = 1f; // 고른 글씨가 꾹 눌린 듯 잠깐 줄었다가 돌아온다.

        // 닫기 시작하는 순간 결정을 알린다. (룬북 확정 연출이 바로 이어지도록)
        Action callback = confirmed ? _onConfirm : _onCancel;
        _onConfirm = null;
        _onCancel = null;
        MarkClosed();
        callback?.Invoke();

        float start = _group != null ? _group.alpha : 1f;
        float elapsed = 0f;
        while (elapsed < _closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            UpdateOptions(Time.unscaledDeltaTime);
            SetAlpha(start * (1f - Mathf.Clamp01(elapsed / _closeDuration)));
            yield return null;
        }

        _routine = null;
        _isClosing = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isOpen || _isClosing)
        {
            return;
        }

        UpdateOptions(Time.unscaledDeltaTime);

        // 연 프레임에는 같은 키 입력을 받지 않는다. (룬북에서 F로 연 경우)
        if (Time.frameCount == _openedFrame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Submit(1);
        }
        else if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space) ||
                 Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Submit(_selected);
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Select(1); // 왼쪽이 취소
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            Select(0); // 오른쪽이 확정
        }
    }

    private void UpdateOptions(float deltaTime)
    {
        _press = Mathf.MoveTowards(_press, 0f, deltaTime / 0.15f);
        ApplyOption(_confirmText, _confirmLine, _selected == 0);
        ApplyOption(_cancelText, _cancelLine, _selected == 1);
    }

    private void ApplyOption(TMP_Text text, Graphic line, bool selected)
    {
        if (text != null)
        {
            text.color = selected ? _selectedColor : _normalColor;
            float scale = selected ? _selectedScale * (1f - 0.08f * _press) : 1f;
            text.rectTransform.localScale = Vector3.one * scale;
        }

        if (line != null)
        {
            line.enabled = selected;
        }
    }

    private void ApplySelection()
    {
        UpdateOptions(0f);
    }

    private void MarkClosed()
    {
        _isOpen = false;
        ClosedFrame = Time.frameCount;
        if (s_openDialog == this)
        {
            s_openDialog = null;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (_group != null)
        {
            _group.alpha = alpha;
        }
    }

    private void SetPanelOffset(float offsetY)
    {
        if (_panel != null)
        {
            _panel.anchoredPosition = _panelBasePosition + new Vector2(0f, offsetY);
        }
    }

    private void RestartRoutine(IEnumerator routine)
    {
        StopRoutine();
        _routine = StartCoroutine(routine);
    }

    private void StopRoutine()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private void OnDisable()
    {
        // 룬북과 함께 꺼지면 결정 없이 닫힌 것으로 정리한다. (꺼지는 중이라 SetActive는 부르지 않는다)
        _routine = null;
        _onConfirm = null;
        _onCancel = null;
        _isClosing = false;
        if (_isOpen)
        {
            MarkClosed();
        }
    }
}
