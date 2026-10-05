using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 확인 창 (Abandon Run? / Quit Game? 등). 제목, 설명, 확인 버튼 문구를 바꿔서 쓴다.
/// 실수로 누르지 않도록 열릴 때 Cancel이 기본 선택된다.
/// </summary>
[DisallowMultipleComponent]
public class ConfirmDialogView : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TMP_Text _confirmLabel;
    [SerializeField] private Button _cancelButton;

    private Action _onConfirm;
    private Action _onCancel;
    private bool _isInitialized;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_confirmButton != null) _confirmButton.onClick.RemoveListener(Confirm);
        if (_cancelButton != null) _cancelButton.onClick.RemoveListener(Cancel);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        if (_confirmButton != null) _confirmButton.onClick.AddListener(Confirm);
        if (_cancelButton != null) _cancelButton.onClick.AddListener(Cancel);
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

        if (EventSystem.current != null && _cancelButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_cancelButton.gameObject);
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
