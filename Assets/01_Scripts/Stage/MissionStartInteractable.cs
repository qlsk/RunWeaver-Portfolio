using UnityEngine;

public class MissionStartInteractable : IInteractable
{
    private readonly StageManager _owner;

    public string InteractionName { get; }
    public Transform InteractionGuidePoint => _owner.transform;

    public MissionStartInteractable(StageManager owner, string interactionName)
    {
        _owner = owner;
        InteractionName = interactionName;
    }

    public void Interact() => _owner.HandleStartActivated();
}