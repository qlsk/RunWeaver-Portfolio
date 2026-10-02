using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float _interactionRange;
    [SerializeField] private LayerMask _interactableLayer;

    private int CompareDistanceToPlayer(Collider2D x, Collider2D y)
    {
        float xDistance = (x.transform.position - transform.position).sqrMagnitude;
        float yDistance = (y.transform.position - transform.position).sqrMagnitude;
        return xDistance.CompareTo(yDistance);
    }

    public InteractionGuideSource FindNearbyTarget()
    {
        Collider2D[] interactableObjects =
            Physics2D.OverlapCircleAll(transform.position, _interactionRange, _interactableLayer);
        if (interactableObjects.Length == 0)
            return null;
        Collider2D closestObject = interactableObjects[0];
        for (int index = 1; index < interactableObjects.Length; index++)
        {
            if (CompareDistanceToPlayer(interactableObjects[index], closestObject) <= 0)
                closestObject = interactableObjects[index];
        }
        var source = closestObject.GetComponent<InteractionGuideSource>();
        return source;
    }
}