using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 일시정지 (기획서 15-8).
///
/// 여는 방법
/// - 열려 있는 화면이 없을 때 Esc. 룬북, 팝업(주 원소 선택, 상점 등)이 열려 있으면 Esc는 그 화면만 닫는다.
/// - 게임 창이 포커스를 잃으면(Alt+Tab 등) 자동으로 연다. 에디터에서는 _pauseOnFocusLostInEditor를 켰을 때만 연다.
///   카운트다운 중에 포커스를 잃으면 메뉴로 돌아간다.
/// - 열 수 없는 때 : 다른 화면이 열려 있을 때, 플레이어 사망 후, 런 종료 후, 화면 전환(ScreenFader) 중, 결과 화면(_blockers)이 켜져 있을 때
///
/// 일시정지 중
/// - Time.timeScale = 0 (적, 탄환, 스폰, 타이머 정지)
/// - PopupController로 열어서 PlayerInputHandler의 게임 입력(이동, 공격, 스킬, 상호작용, 룬북)을 막는다.
///
/// 메뉴
/// - 탐사 중 : Resume / Settings / Abandon Run / Quit Game
/// - 거점 : Resume / Settings / Quit Game
/// - Resume(Esc) : 탐사 중에는 3, 2, 1 카운트다운 후 재개, 거점에서는 바로 재개
///   카운트다운 중에 Esc를 누르면 메뉴로 돌아간다.
/// - Abandon Run : 경고 창 → 확인하면 이번 런을 포기한다. (종료 사유 Abandoned를 기록하고 사망 처리 → "Run Abandoned" 결과)
/// - Quit Game : 확인 창 → 확인하면 게임 종료
///
/// 이 컴포넌트는 항상 켜져 있는 오브젝트에 붙이고, 화면(_content)은 꺼진 상태로 둔다.
/// </summary>
[DisallowMultipleComponent]
public class PauseController : MonoBehaviour
{
    private enum State
    {
        Closed,
        Menu,
        Settings,
        Confirm,
        Countdown
    }

    [Header("Scene")]
    [Tooltip("탐사 중(전투 씬)이면 켠다. 꺼져 있으면 거점 메뉴(Abandon Run 없음)를 보여준다.")]
    [SerializeField] private bool _isExpedition;
    [Tooltip("비워두면 씬에서 찾는다")]
    [SerializeField] private PopupController _popupController;
    [Tooltip("비워두면 씬에서 찾는다 (탐사 포기 시 사망 처리, 사망 후 일시정지 금지)")]
    [SerializeField] private PlayerHealth _playerHealth;
    [Tooltip("비워두면 씬에서 찾는다 (룬북이 열려 있으면 일시정지를 열지 않는다)")]
    [SerializeField] private RuneBookPauseController _runeBookPause;
    [Tooltip("이 중 하나라도 켜져 있으면 일시정지를 열 수 없다 (결과 화면 등)")]
    [SerializeField] private GameObject[] _blockers;

    [Header("UI")]
    [Tooltip("일시정지 화면 전체 (어두운 배경 포함). 꺼진 상태로 둔다.")]
    [SerializeField] private GameObject _content;
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private TMP_Text _subtitleText;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _abandonButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private SettingsPanelView _settingsPanel;
    [SerializeField] private ConfirmDialogView _confirmDialog;
    [SerializeField] private CountdownView _countdown;
    [Tooltip("카운트다운 중 배경을 조금 밝게 한다")]
    [SerializeField] private Image _dim;
    [SerializeField, Range(0f, 1f)] private float _menuDimAlpha = 0.7f;
    [SerializeField, Range(0f, 1f)] private float _countdownDimAlpha = 0.35f;

    [Header("Settings")]
    [SerializeField, Min(1)] private int _countdownSeconds = 3;
    [Tooltip("에디터에서도 게임 창이 포커스를 잃으면 일시정지한다. (끄면 Console 등을 눌러도 멈추지 않는다. 빌드에서는 항상 일시정지)")]
    [SerializeField] private bool _pauseOnFocusLostInEditor;

    private State _state = State.Closed;
    private float _previousTimeScale = 1f;
    private int _openedFrame = -1;
    private bool _wasBlockedLastFrame;

    /// <summary>일시정지 화면이 열려 있는지 (카운트다운 포함)</summary>
    public static bool IsGamePaused { get; private set; }

    private void Awake()
    {
        if (_popupController == null) _popupController = FindAnyObjectByType<PopupController>();
        if (_playerHealth == null) _playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (_runeBookPause == null) _runeBookPause = FindAnyObjectByType<RuneBookPauseController>();

        if (_resumeButton != null) _resumeButton.onClick.AddListener(Resume);
        if (_settingsButton != null) _settingsButton.onClick.AddListener(OpenSettings);
        if (_abandonButton != null) _abandonButton.onClick.AddListener(AskAbandonRun);
        if (_quitButton != null) _quitButton.onClick.AddListener(AskQuitGame);
        if (_settingsPanel != null) _settingsPanel.Closed += HandleSettingsClosed;

        HideAll();
        if (_content != null)
        {
            _content.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_settingsPanel != null) _settingsPanel.Closed -= HandleSettingsClosed;

        // 일시정지 중에 씬이 바뀌어도 게임이 멈춘 채로 남지 않게 한다.
        if (_state != State.Closed)
        {
            Time.timeScale = _previousTimeScale;
            IsGamePaused = false;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || Time.frameCount == _openedFrame)
        {
            return;
        }

        switch (_state)
        {
            case State.Closed:
                // 같은 Esc로 룬북/팝업이 닫힌 프레임에는 열지 않는다.
                if (!_wasBlockedLastFrame)
                {
                    Open();
                }
                break;
            case State.Menu:
                Resume();
                break;
            case State.Settings:
                _settingsPanel.Cancel();
                break;
            case State.Confirm:
                _confirmDialog.Cancel();
                break;
            case State.Countdown:
                // 카운트다운을 멈추고 메뉴로 돌아간다. (게임은 계속 멈춰 있다)
                ShowMenu();
                break;
        }
    }

    private void LateUpdate()
    {
        _wasBlockedLastFrame = _state == State.Closed && IsBlockedByOtherScreen();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            return;
        }

        if (Application.isEditor && !_pauseOnFocusLostInEditor)
        {
            return;
        }

        if (_state == State.Closed)
        {
            Open();
        }
        else if (_state == State.Countdown)
        {
            // 카운트다운 중에 자리를 비우면 재개되지 않도록 메뉴로 돌아간다.
            ShowMenu();
        }
    }

    // ───────────────────────── 열기 / 닫기 ─────────────────────────

    public bool CanOpen()
    {
        if (_state != State.Closed || _content == null || IsBlockedByOtherScreen())
        {
            return false;
        }

        // 런이 끝난 뒤(클리어 연출, 결과창)에는 열지 않는다.
        if (RunFlow.HasEnded)
        {
            return false;
        }

        // 층 전환 연출(검은 화면) 중에는 열지 않는다.
        if (ScreenFader.IsFading)
        {
            return false;
        }

        return _playerHealth == null || !_playerHealth.IsDead;
    }

    public void Open()
    {
        if (!CanOpen())
        {
            return;
        }

        _openedFrame = Time.frameCount;
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        IsGamePaused = true;

        if (_popupController != null)
        {
            _popupController.OpenPopup(_content);
        }
        else
        {
            _content.SetActive(true);
        }

        ShowMenu();
    }

    /// <summary>탐사 중에는 3, 2, 1 카운트다운 후 재개하고, 거점에서는 바로 재개한다.</summary>
    public void Resume()
    {
        if (_state != State.Menu)
        {
            return;
        }

        if (!_isExpedition)
        {
            Close();
            return;
        }

        HideAll();
        _state = State.Countdown;
        SetDimAlpha(_countdownDimAlpha);

        if (_countdown != null)
        {
            _countdown.Play(_countdownSeconds, Close);
        }
        else
        {
            Close();
        }
    }

    /// <summary>카운트다운 없이 바로 닫고 게임 시간을 되돌린다.</summary>
    private void Close()
    {
        if (_state == State.Closed)
        {
            return;
        }

        HideAll();
        _state = State.Closed;
        Time.timeScale = _previousTimeScale;
        IsGamePaused = false;

        if (_popupController != null)
        {
            _popupController.ClosePopup();
        }
        else if (_content != null)
        {
            _content.SetActive(false);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // ───────────────────────── 메뉴 ─────────────────────────

    private void ShowMenu()
    {
        HideAll();
        _state = State.Menu;
        SetDimAlpha(_menuDimAlpha);

        if (_menuPanel != null) _menuPanel.SetActive(true);
        if (_abandonButton != null) _abandonButton.gameObject.SetActive(_isExpedition);
        if (_subtitleText != null) _subtitleText.text = GetSubtitle();

        if (EventSystem.current != null && _resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_resumeButton.gameObject);
        }
    }

    private void OpenSettings()
    {
        if (_state != State.Menu || _settingsPanel == null)
        {
            return;
        }

        HideAll();
        _state = State.Settings;
        _settingsPanel.Open();
    }

    private void HandleSettingsClosed()
    {
        if (_state == State.Settings)
        {
            ShowMenu();
        }
    }

    private void AskAbandonRun()
    {
        if (_state != State.Menu || !_isExpedition)
        {
            return;
        }

        ShowConfirm("탐사를 포기할까요?",
            "이번 탐사가 여기서 <color=#FF6B5A>끝납니다</color>.\n장착한 룬과 골드를 잃게 됩니다.",
            "포기",
            AbandonRun);
    }

    private void AskQuitGame()
    {
        if (_state != State.Menu)
        {
            return;
        }

        string body = _isExpedition
            ? "현재 탐사는 <color=#FF6B5A>저장되지 않습니다</color>.\n게임을 종료할까요?"
            : "게임을 종료할까요?";
        ShowConfirm("게임 종료", body, "종료", QuitGame);
    }

    private void ShowConfirm(string title, string body, string confirmLabel, System.Action onConfirm)
    {
        if (_confirmDialog == null)
        {
            onConfirm?.Invoke();
            return;
        }

        HideAll();
        _state = State.Confirm;
        _confirmDialog.Show(title, body, confirmLabel, onConfirm, ShowMenu);
    }

    private void AbandonRun()
    {
        // 게임 시간을 되돌린 뒤 사망 처리한다. (사망 연출과 결과 화면은 기존 사망 흐름을 그대로 탄다)
        // 종료 사유를 먼저 기록해 결과에 "Run Abandoned"가 나오게 한다.
        Close();
        RunFlow.TrySetEndReason(RunEndReason.Abandoned);

        if (_playerHealth != null)
        {
            _playerHealth.Kill();
        }
        else
        {
            Debug.LogWarning("[PauseController] PlayerHealth가 없어 탐사 포기를 처리하지 못했습니다.", this);
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ───────────────────────── 도움 함수 ─────────────────────────

    private bool IsBlockedByOtherScreen()
    {
        if (_popupController != null && _popupController.IsOpen)
        {
            return true;
        }

        if (_runeBookPause != null && _runeBookPause.IsPaused)
        {
            return true;
        }

        // 다른 무언가가 이미 게임을 멈춰 둔 경우 (연출 등)
        if (Time.timeScale <= 0f)
        {
            return true;
        }

        if (_blockers != null)
        {
            foreach (GameObject blocker in _blockers)
            {
                if (blocker != null && blocker.activeInHierarchy)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string GetSubtitle()
    {
        if (!_isExpedition)
        {
            return "마을";
        }

        int depth = StageManager.Instance != null ? StageManager.Instance.CurrentDepth : 1;
        return $"탐사 · {depth}층";
    }

    private void HideAll()
    {
        if (_menuPanel != null) _menuPanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.gameObject.SetActive(false);
        if (_confirmDialog != null) _confirmDialog.Hide();
        if (_countdown != null) _countdown.Stop();
    }

    private void SetDimAlpha(float alpha)
    {
        if (_dim == null)
        {
            return;
        }

        Color color = _dim.color;
        color.a = alpha;
        _dim.color = color;
    }
}
