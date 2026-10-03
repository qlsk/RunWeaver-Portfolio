using UnityEngine;

public class InteractionPopupController : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField] private PlayerInteraction _playerInteraction;
    [SerializeField] private GameObject _primaryElementSelectionPopup;

    private void Start()
    {
        _playerInputHandler.InteractionRequested += Popup;
    }

    private void Popup() { }
}