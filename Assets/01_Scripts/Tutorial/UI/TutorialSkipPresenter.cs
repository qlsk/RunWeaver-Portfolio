using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 메뉴의 "튜토리얼 건너뛰기" 항목.
/// 누르면 일시정지 메뉴의 확인 창(탐사 포기, 게임 종료와 같은 창)으로 묻는다.
/// 확인 창이 떠 있는 동안 메뉴는 숨고 입력을 받지 않으며, 취소(Esc)하면 이 항목이 다시 선택된다.
/// 튜토리얼이 진행 중일 때만 항목이 보인다.
/// </summary>
public class TutorialSkipPresenter : MonoBehaviour
{
    [SerializeField] private TutorialController _controller;
    [SerializeField] private Button _menuButton;
    [Tooltip("예전 전용 확인 창. 쓰지 않으며 켜져 있으면 끈다 (비워도 된다)")]
    [SerializeField] private GameObject _dialog;
    [SerializeField] private PauseController _pauseController;

    [Header("확인 창 문구")]
    [SerializeField] private string _title = "튜토리얼을 건너뛸까요?";
    [SerializeField, TextArea] private string _body = "건너뛰면 바로 탐사를 시작합니다.\n안내는 <color=#2F5E8C>협회 지침서</color>에서 언제든 다시 볼 수 있어요.";
    [SerializeField] private string _confirmLabel = "건너뛰기";

    private void Awake()
    {
        if (_dialog != null)
            _dialog.SetActive(false);
    }

    private void OnEnable()
    {
        if (_menuButton != null)
            _menuButton.onClick.AddListener(HandleMenuClicked);
    }

    private void OnDisable()
    {
        if (_menuButton != null)
            _menuButton.onClick.RemoveListener(HandleMenuClicked);
    }

    private void LateUpdate()
    {
        if (_menuButton == null)
            return;

        bool isTutorialRunning = _controller != null && _controller.IsRunning;
        if (_menuButton.gameObject.activeSelf != isTutorialRunning)
            _menuButton.gameObject.SetActive(isTutorialRunning);
    }

    private void HandleMenuClicked()
    {
        if (_pauseController == null)
        {
            Skip();
            return;
        }

        _pauseController.ShowMenuConfirm(_title, _body, _confirmLabel, Skip, _menuButton.gameObject);
    }

    private void Skip()
    {
        if (_controller != null)
            _controller.Skip();
    }
}
