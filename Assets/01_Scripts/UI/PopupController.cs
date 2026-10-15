using System;
using UnityEngine;

public class PopupController : MonoBehaviour
{
    public event Action<bool> PopupOpenStateChanged;

    private GameObject _currentPopup;

    /// <summary>팝업이 하나라도 열려 있는지</summary>
    public bool IsOpen => _currentPopup != null;

    public void OpenPopup(GameObject popup)
    {
        if (_currentPopup == popup)
            return;
        if (_currentPopup != null)
            _currentPopup.SetActive(false);
        _currentPopup = popup;
        _currentPopup.SetActive(true);
        PopupOpenStateChanged?.Invoke(true);
    }

    public void ClosePopup()
    {
        if (_currentPopup == null)
            return;
        _currentPopup.SetActive(false);
        _currentPopup = null;
        PopupOpenStateChanged?.Invoke(false);
    }
}