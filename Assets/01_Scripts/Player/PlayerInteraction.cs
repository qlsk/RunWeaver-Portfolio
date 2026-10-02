using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputHandler _playerInputHandler;

    [Header("Settings")]
    [SerializeField] private float _interactionRange;
    [SerializeField] private LayerMask _interactableLayer;

    public InteractionGuideSource CurrentTarget { get; private set; }

    public event Action<InteractionGuideSource> InteractionTargetChanged;

    public void Update()
    {
        RefreshTarget();
    }

    private void RefreshTarget()
    {
        Collider2D[] interactableObjects =
            Physics2D.OverlapCircleAll(transform.position, _interactionRange, _interactableLayer);
        InteractionGuideSource nearestSource = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (Collider2D interactableObject in interactableObjects)
        {
            var source = interactableObject.GetComponent<InteractionGuideSource>();
            if (source == null || !source.isActiveAndEnabled)
                continue;
            float distance = (source.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSource = source;
            }
            if (CurrentTarget == nearestSource)
                return;
            CurrentTarget = nearestSource;
            InteractionTargetChanged?.Invoke(source);
        }
    }
}