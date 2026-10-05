using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputHandler _playerInputHandler;

    [Header("Settings")]
    [SerializeField] private float _interactionRange;
    [SerializeField] private LayerMask _interactableLayer;

    private IInteractable _currentInteractable;

    public event Action<IInteractable> CurrentInteractableChanged;

    private void OnEnable()
    {
        _playerInputHandler.InteractionRequested += OnInteractionRequested;
    }

    private void OnDisable()
    {
        _playerInputHandler.InteractionRequested -= OnInteractionRequested;
    }

    private void Update()
    {
        FindNearestInteractable();
    }

    private void OnInteractionRequested()
    {
        // 주변에 대상이 없거나 이미 파괴된 대상이면 무시한다.
        if (_currentInteractable == null ||
            (_currentInteractable is UnityEngine.Object target && target == null))
            return;
        _currentInteractable.Interact();
    }

    private void FindNearestInteractable()
    {
        Collider2D[] interactableObjects =
            Physics2D.OverlapCircleAll(transform.position, _interactionRange, _interactableLayer);
        IInteractable nearestInteractable = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (Collider2D interactableObject in interactableObjects)
        {
            if (!interactableObject.TryGetComponent(out IInteractable interactable))
                continue;
            float distance = Vector3.Distance(interactableObject.transform.position, transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestInteractable = interactable;
            }
        }
        if (_currentInteractable == nearestInteractable)
            return;
        _currentInteractable = nearestInteractable;
        CurrentInteractableChanged?.Invoke(_currentInteractable);
    }
}