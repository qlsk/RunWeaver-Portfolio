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
    private IInteractable _fallbackInteractable;

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
        if (_currentInteractable == null || (_currentInteractable is UnityEngine.Object target && target == null))
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

        if (nearestInteractable == null && IsAlive(_fallbackInteractable))
            nearestInteractable = _fallbackInteractable;

        if (_currentInteractable == nearestInteractable)
            return;

        _currentInteractable = nearestInteractable;
        CurrentInteractableChanged?.Invoke(_currentInteractable);
    }

    private static bool IsAlive(IInteractable interactable)
    {
        bool isNotDestroyed = interactable is not UnityEngine.Object target || target != null;
        return interactable != null && isNotDestroyed;
    }

    /// 근처에 다른 상호작용 대상이 없을 때 맵 어디서든 상호작용할 수 있는 대상을 등록한다.
    public void SetFallbackInteractable(IInteractable interactable)
    {
        _fallbackInteractable = interactable;
    }

    /// 등록된 대체 상호작용 대상이 현재 상호작용 대상과 같을 경우 비운다.
    public void ClearFallbackInteractable(IInteractable interactable)
    {
        if (ReferenceEquals(_fallbackInteractable, interactable))
            _fallbackInteractable = null;
    }
}