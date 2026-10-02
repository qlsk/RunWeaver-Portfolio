using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionGuide : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private RectTransform _canvas;
    [SerializeField] private PlayerInteraction _playerInteraction;
    [SerializeField] private RectTransform _interactionGuide;
    [SerializeField] private Image _interactionKey;
    [SerializeField] private TextMeshProUGUI _interactionName;

    private void Update()
    {
        InteractionGuideSource source = _playerInteraction.FindNearbyTarget();
        Refresh(source);
    }

    private void Refresh(InteractionGuideSource source)
    {
        if (source == null)
        {
            _interactionGuide.gameObject.SetActive(false);
            return;
        }

        _interactionName.text = source.InteractionName;
        Vector3 screenPoint = _camera.WorldToScreenPoint(source.GuidePoint.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas,
            screenPoint,
            _camera,
            out Vector2 localPosition);
        _interactionGuide.anchoredPosition = localPosition;
        _interactionGuide.gameObject.SetActive(true);
    }
}