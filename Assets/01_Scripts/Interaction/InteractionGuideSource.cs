using UnityEngine;

public class InteractionGuideSource : MonoBehaviour
{
    [SerializeField] private Transform _guidePoint;
    [SerializeField] private string _interactionName;

    public Transform GuidePoint => _guidePoint;
    public string InteractionName => _interactionName;
}