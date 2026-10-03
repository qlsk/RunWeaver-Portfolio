using System;
using UnityEngine;

public class PopupController : MonoBehaviour
{
    public event Action<bool> PopupOpenStateChanged;

    public void OpenPopup(GameObject popup)
    {
        popup.SetActive(true);
        PopupOpenStateChanged?.Invoke(true);
    }
}