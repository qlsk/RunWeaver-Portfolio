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

    private Button _continueButton;
    private Button _skipButton;

    private void Awake()
    {
        if (_dialog != null)
        {
            _continueButton = FindButton(_dialog.transform, ContinueButtonName);
            _skipButton = FindButton(_dialog.transform, SkipButtonName);
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
    }

    private void OnDisable()
    {
        if (_menuButton != null)
            _menuButton.onClick.RemoveListener(HandleMenuClicked);

        if (_continueButton != null)
            _continueButton.onClick.RemoveListener(HandleContinueClicked);

        if (_skipButton != null)
            _skipButton.onClick.RemoveListener(HandleSkipClicked);
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

    private void HandleMenuClicked()
    {
        if (_dialog != null)
            _dialog.SetActive(true);
    }

    private void HandleContinueClicked()
    {
        _dialog.SetActive(false);
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
