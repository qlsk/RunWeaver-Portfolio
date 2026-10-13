using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TutorialSkipPresenter : MonoBehaviour
{
    private const string ContinueButtonName = "ContinueButton";
    private const string SkipButtonName = "SkipButton";

    [SerializeField] private TutorialController _controller;
    [SerializeField] private Button _menuButton;
    [SerializeField] private GameObject _dialog;
    [SerializeField] private PauseController _pauseController;

    private Button _continueButton;
    private Button _skipButton;

    private void Awake()
    {
        if (_dialog == null)
            return;
        _continueButton = FindButton(_dialog.transform, ContinueButtonName);
        _skipButton = FindButton(_dialog.transform, SkipButtonName);
        SetDialogNavigation();
        _dialog.SetActive(false);
    }

    private void OnEnable()
    {
        _menuButton.onClick.AddListener(HandleMenuClicked);
        _continueButton.onClick.AddListener(HandleContinueClicked);
        _skipButton.onClick.AddListener(HandleSkipClicked);
        _pauseController.RegisterSubScreenCloseHandler(HandleEscape);
    }

    private void OnDisable()
    {
        _menuButton.onClick.RemoveListener(HandleMenuClicked);
        _continueButton.onClick.RemoveListener(HandleContinueClicked);
        _skipButton.onClick.RemoveListener(HandleSkipClicked);
        _pauseController.UnregisterSubScreenCloseHandler(HandleEscape);
    }

    private void LateUpdate()
    {
        if (_menuButton == null)
            return;

        bool isTutorialRunning = _controller != null && _controller.IsRunning;
        if (_menuButton.gameObject.activeSelf != isTutorialRunning)
            _menuButton.gameObject.SetActive(isTutorialRunning);

        if (_dialog != null && _dialog.activeSelf && !_menuButton.gameObject.activeInHierarchy)
            _dialog.SetActive(false);
    }

    private bool HandleEscape()
    {
        if (_dialog == null || !_dialog.activeSelf)
            return false;

        CloseDialog();
        return true;
    }

    private void CloseDialog()
    {
        _dialog.SetActive(false);

        if (_menuButton != null && _menuButton.gameObject.activeInHierarchy)
            SetEventSelection(_menuButton.gameObject);
    }

    private void SetDialogNavigation()
    {
        if (_continueButton == null || _skipButton == null)
            return;

        _continueButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _skipButton };
        _skipButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _continueButton };
    }

    private static void SetEventSelection(GameObject target)
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(target);
    }

    private void HandleMenuClicked()
    {
        if (_dialog == null)
            return;

        _dialog.SetActive(true);

        if (_continueButton != null)
            SetEventSelection(_continueButton.gameObject);
    }

    private void HandleContinueClicked()
    {
        CloseDialog();
    }

    private void HandleSkipClicked()
    {
        _dialog.SetActive(false);

        if (_controller != null)
            _controller.Skip();
    }

    private static Button FindButton(Transform parent, string buttonName)
    {
        return parent.GetComponentsInChildren<Button>(true).FirstOrDefault(button => button.name == buttonName);
    }
}