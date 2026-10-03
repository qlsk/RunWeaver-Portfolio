using UnityEngine;

public class RuneEffectSelectionUIController : MonoBehaviour
{
    [SerializeField] private PopupController _popupController;
    [SerializeField] private MainElementSelectView _primaryElementSelectionView;
    [SerializeField] private GameObject _runeEffectActivationPopup;

    private void OnEnable()
    {
        _primaryElementSelectionView.Confirmed += OnPrimaryElementConfirmed;
        _primaryElementSelectionView.Cancelled += OnPrimaryElementCancelled;
    }

    private void OnDisable()
    {
        _primaryElementSelectionView.Confirmed -= OnPrimaryElementConfirmed;
        _primaryElementSelectionView.Cancelled -= OnPrimaryElementCancelled;
    }

    private void OnPrimaryElementConfirmed(ReactionElement element)
    {
        _popupController.OpenPopup(_runeEffectActivationPopup);
    }

    private void OnPrimaryElementCancelled()
    {
        _popupController.ClosePopup();
    }
}