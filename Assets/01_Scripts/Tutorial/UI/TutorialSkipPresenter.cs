using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 메뉴의 "튜토리얼 건너뛰기" 항목과 확인 대화상자를 다룬다.
/// 튜토리얼이 진행 중일 때만 항목을 보여주고, 건너뛰기를 확인하면 컨트롤러의 Skip을 부른다.
/// </summary>
public class TutorialSkipPresenter : MonoBehaviour
{
    private const string ContinueButtonName = "ContinueButton";
    private const string SkipButtonName = "SkipButton";

    [SerializeField] private TutorialController _controller;
    [Tooltip("일시정지 메뉴 안의 건너뛰기 항목 버튼")]
    [SerializeField] private Button _menuButton;
    [Tooltip("확인 대화상자(TutorialSkipDialog 프리팹 인스턴스). 처음에는 꺼 둔다")]
    [SerializeField] private GameObject _dialog;
    [Tooltip("대화상자가 열려 있을 때 Esc가 메뉴 전체를 닫지 않고 대화상자만 닫게 한다")]
    [SerializeField] private PauseController _pauseController;

    private Button _continueButton;
    private Button _skipButton;

    private void Awake()
    {
        if (_dialog != null)
        {
            _continueButton = FindButton(_dialog.transform, ContinueButtonName);
            _skipButton = FindButton(_dialog.transform, SkipButtonName);
            SetDialogNavigation();
            _dialog.SetActive(false);
        }

        if (_menuButton == null || _dialog == null || _continueButton == null || _skipButton == null)
            Debug.LogError("[TutorialSkipPresenter] 건너뛰기 항목 또는 대화상자 참조가 연결되지 않았습니다.", this);
    }

    private void OnEnable()
    {
        if (_menuButton != null)
            _menuButton.onClick.AddListener(HandleMenuClicked);

        if (_continueButton != null)
            _continueButton.onClick.AddListener(HandleContinueClicked);

        if (_skipButton != null)
            _skipButton.onClick.AddListener(HandleSkipClicked);

        if (_pauseController != null)
            _pauseController.RegisterSubScreenCloseHandler(HandleEscape);
    }

    private void OnDisable()
    {
        if (_menuButton != null)
            _menuButton.onClick.RemoveListener(HandleMenuClicked);

        if (_continueButton != null)
            _continueButton.onClick.RemoveListener(HandleContinueClicked);

        if (_skipButton != null)
            _skipButton.onClick.RemoveListener(HandleSkipClicked);

        if (_pauseController != null)
            _pauseController.UnregisterSubScreenCloseHandler(HandleEscape);
    }

    private void LateUpdate()
    {
        if (_menuButton == null)
            return;

        // 튜토리얼이 진행 중일 때만 항목을 보여준다. 일시정지 메뉴가 닫혀 있으면 대화상자도 닫는다.
        bool isTutorialRunning = _controller != null && _controller.IsRunning;
        if (_menuButton.gameObject.activeSelf != isTutorialRunning)
            _menuButton.gameObject.SetActive(isTutorialRunning);

        if (_dialog != null && _dialog.activeSelf && !_menuButton.gameObject.activeInHierarchy)
            _dialog.SetActive(false);
    }

    /// <summary>대화상자가 열려 있으면 닫고 true를 돌려준다.</summary>
    private bool HandleEscape()
    {
        if (_dialog == null || !_dialog.activeSelf)
            return false;

        CloseDialog();
        return true;
    }

    /// <summary>대화상자를 닫고 키보드 선택을 건너뛰기 메뉴 항목으로 되돌린다.</summary>
    private void CloseDialog()
    {
        _dialog.SetActive(false);

        if (_menuButton != null && _menuButton.gameObject.activeInHierarchy)
            SetEventSelection(_menuButton.gameObject);
    }

    /// <summary>두 버튼 사이만 좌우로 오가게 해 방향키가 뒤쪽 메뉴 버튼으로 새지 않게 한다.</summary>
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

        // 키보드 입력이 뒤쪽 메뉴 대신 대화상자를 조작하도록 선택을 옮긴다. 기본은 안전한 "계속 진행"이다.
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
        foreach (Button button in parent.GetComponentsInChildren<Button>(true))
        {
            if (button.name == buttonName)
                return button;
        }

        return null;
    }
}
