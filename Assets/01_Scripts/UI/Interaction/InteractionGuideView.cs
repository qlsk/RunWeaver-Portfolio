using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionGuideView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private RectTransform _canvas;
    [SerializeField] private PlayerInteraction _playerInteraction;

    [Header("UI References")]
    [SerializeField] private RectTransform _interactionGuideUI;
    [SerializeField] private Image _interactionKeyUI;
    [SerializeField] private TextMeshProUGUI _interactionNameUI;

    private IInteractable _nearestInteractable;

    private void OnEnable()
    {
        _playerInteraction.CurrentInteractableChanged += OnCurrentInteractableChanged;
    }

    private void OnDisable()
    {
        _playerInteraction.CurrentInteractableChanged -= OnCurrentInteractableChanged;
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void OnCurrentInteractableChanged(IInteractable interactable)
    {
        _nearestInteractable = interactable;
        Refresh();
    }

    private void Refresh()
    {
        if (_nearestInteractable == null)
        {
            _interactionGuideUI.gameObject.SetActive(false);
            return;
        }
        _interactionNameUI.text = _nearestInteractable.InteractionName;
        Vector3 screenPoint = _camera.WorldToScreenPoint(_nearestInteractable.InteractionGuidePoint.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screenPoint, _camera,
            out Vector2 localPosition);
        _interactionGuideUI.anchoredPosition = localPosition;
        _interactionGuideUI.gameObject.SetActive(true);
    }
}