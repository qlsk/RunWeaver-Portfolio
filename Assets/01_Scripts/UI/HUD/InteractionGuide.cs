using System;
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

    private InteractionGuideSource _currentTarget;

    private void OnEnable()
    {
        _playerInteraction.InteractionTargetChanged += OnTargetChanged;
        OnTargetChanged(_playerInteraction.CurrentTarget);
    }

    private void OnDisable()
    {
        _playerInteraction.InteractionTargetChanged -= OnTargetChanged;
        _currentTarget = null;
        _interactionGuide.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void OnTargetChanged(InteractionGuideSource target)
    {
        _currentTarget = target;
        if (target != null)
            _interactionName.text = target.InteractionName;
        Refresh();
    }

    private void Refresh()
    {
        if (_currentTarget == null)
        {
            _interactionGuide.gameObject.SetActive(false);
            return;
        }
        _interactionName.text = _currentTarget.InteractionName;
        Vector3 screenPoint = _camera.WorldToScreenPoint(_currentTarget.GuidePoint.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas,
            screenPoint,
            _camera,
            out Vector2 localPosition);
        _interactionGuide.anchoredPosition = localPosition;
        _interactionGuide.gameObject.SetActive(true);
    }
}