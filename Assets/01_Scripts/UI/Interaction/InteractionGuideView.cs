using System;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionGuideView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private RectTransform _canvas;
    [SerializeField] private PlayerInteraction _playerInteraction;

    [Header("UI References")]
    [SerializeField] private RectTransform _interactionGuideUI;
    [SerializeField] private Image _interactionKeyUI;
    [SerializeField] private TextMeshProUGUI _interactionNameUI;

    [Header("Fade")]
    [SerializeField] private MMF_Player _fadeIn;
    [SerializeField] private MMF_Player _fadeOut;

    private bool _isHiding;

    private IInteractable _nearestInteractable;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_playerInteraction == null)
            _playerInteraction = FindAnyObjectByType<PlayerInteraction>();
    }

    private void OnEnable()
    {
        if (_playerInteraction != null)
            _playerInteraction.CurrentInteractableChanged += OnCurrentInteractableChanged;
        _fadeOut.Events.OnComplete.AddListener(OnFadeOutCompleted);
    }

    private void OnDisable()
    {
        if (_playerInteraction != null)
            _playerInteraction.CurrentInteractableChanged -= OnCurrentInteractableChanged;
        _fadeOut.Events.OnComplete.RemoveListener(OnFadeOutCompleted);
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void OnCurrentInteractableChanged(IInteractable interactable)
    {
        _nearestInteractable = interactable;
        Refresh();
    }

    private void Refresh()
    {
        // 이미 파괴된 대상(들어간 포탈 등)도 없는 것으로 본다.
        if (_nearestInteractable == null || (_nearestInteractable is UnityEngine.Object target && target == null))
        {
            Hide();
            return;
        }

        _interactionNameUI.text = _nearestInteractable.InteractionName;
        Show();
    }

    private void Show()
    {
        if (_isHiding)
        {
            _isHiding = false;
            _fadeOut.StopFeedbacks();
            _fadeIn.PlayFeedbacks();
            return;
        }

        if (_interactionGuideUI.gameObject.activeSelf)
            return;

        _interactionGuideUI.gameObject.SetActive(true);
        _fadeIn.PlayFeedbacks();
    }

    private void Hide()
    {
        if (_isHiding)
            return;

        if (!_interactionGuideUI.gameObject.activeSelf)
            return;

        _isHiding = true;
        _fadeIn.StopFeedbacks();
        _fadeOut.PlayFeedbacks();
    }

    private void OnFadeOutCompleted()
    {
        if (!_isHiding)
            return;

        _isHiding = false;
        _interactionGuideUI.gameObject.SetActive(false);
    }
}