using UnityEngine;

public class InteractionGuideSource : MonoBehaviour
{
    [SerializeField] private Transform _guidePoint;
    [SerializeField] private string _interactionName;
    [SerializeField] private GameObject _popup;

    public Transform GuidePoint => _guidePoint;
    public string InteractionName => _interactionName;
    public GameObject Popup => _popup;
}