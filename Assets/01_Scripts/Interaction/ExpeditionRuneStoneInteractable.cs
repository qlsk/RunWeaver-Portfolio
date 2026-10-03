using UnityEngine;

public class ExpeditionRuneStoneInteractable : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private PopupController _popupController;

    [Header("Settings")]
    [SerializeField] private string _interactionName;
    [SerializeField] private Transform _interactionGuidePoint;
    [SerializeField] private GameObject _primaryElementSelectionPopup;

    public string InteractionName => _interactionName;
    public Transform InteractionGuidePoint => _interactionGuidePoint;

    public void Interact()
    {
        _popupController.OpenPopup(_primaryElementSelectionPopup);
    }
}