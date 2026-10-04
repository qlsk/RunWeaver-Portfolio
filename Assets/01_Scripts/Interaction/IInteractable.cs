using UnityEngine;

public interface IInteractable
{
    public string InteractionName { get; }
    public Transform InteractionGuidePoint { get; }

    public void Interact();
}