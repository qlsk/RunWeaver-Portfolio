using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 타이틀(StartScene) 메뉴를 연결한다. 메뉴 항목 순서는 아래와 같다.
/// - 0 새 게임 : 새 게임을 시작하고, 시작 연출(TitleSequence)이 끝나면 로딩 화면(LoadingScene)을 거쳐 전투 씬으로 간다.
/// - 1 이어하기 : 저장 기능이 없어 지금은 누를 수 없다. (메뉴 항목에서 Disabled)
/// - 2 설정 : 설정 창(SettingsUI)을 연다. 창이 열린 동안 메뉴는 잠기고, Esc는 취소와 같다.
/// - 3 게임 종료 : 게임을 끝낸다.
/// </summary>
[DisallowMultipleComponent]
public class TitleMenuController : MonoBehaviour
{
    private const int NewGameIndex = 0;
    private const int SettingsIndex = 2;
    private const int QuitIndex = 3;

    [SerializeField] private TitleMenuView _menu;
    [SerializeField] private TitleSequence _sequence;

    [Tooltip("시작 버튼을 누르면 갈 로딩 씬. 로딩 씬이 데이터를 불러온 뒤 전투 씬으로 넘어간다.")]
    [SerializeField] private string _loadingSceneName = "LoadingScene";

    [Header("설정 창 (SettingsUI 루트)")]
    [SerializeField] private GameObject _settingsPanel;

    [Header("BGM")]
    [SerializeField] private TitleBgm _bgm;

    private SettingsPanelView _settingsView;
    private bool _isStarting;

    private void Awake()
    {
        _settingsView = _settingsPanel != null ? _settingsPanel.GetComponentInChildren<SettingsPanelView>(true) : null;

        if (_menu != null) _menu.Confirmed += HandleConfirmed;
        if (_settingsView != null) _settingsView.Closed += HandleSettingsClosed;

        if (_menu != null)
            _menu.Moved += HandleMoved;
    }

    private void OnDestroy()
    {
        if (_menu != null) _menu.Confirmed -= HandleConfirmed;
        if (_settingsView != null) _settingsView.Closed -= HandleSettingsClosed;

        if (_menu != null)
            _menu.Moved -= HandleMoved;
    }

    private void Update()
    {
        if (_settingsView != null && _settingsView.IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            _settingsView.Cancel();
        }
    }

    private static void HandleMoved()
    {
        // 메뉴 이동음을 한 번 재생한다.
        Sfx.Play(Sfx.MenuMove);
    }

    private void HandleConfirmed(int index)
    {
        // 확정음을 한 번 재생한다.
        Sfx.Play(Sfx.MenuConfirm);

        // 항목별 분기를 수행한다.
        switch (index)
        {
            case NewGameIndex:
                StartNewGame();
                break;
            case SettingsIndex:
                OpenSettings();
                break;
            case QuitIndex:
                QuitGame();
                break;
            default:
                if (_menu != null) _menu.SetLocked(false);
                break;
        }
    }

    private void StartNewGame()
    {
        // 이미 시작 중이면 종료한다.
        if (_isStarting)
        {
            return;
        }

        // 시작 중 상태로 전환한다.
        _isStarting = true;

        // BGM 페이드아웃을 요청한다.
        _bgm.FadeOut();

        GameSession session = GameSession.Instance;
        if (session != null && session.IsReady && !session.Current.Holdings.IsOnExpedition)
        {
            session.StartNewGame();
        }

        OwnedRuneWords.Clear();

        if (_sequence != null)
        {
            _sequence.PlayStart(NewGameIndex, LoadNextScene);
        }
        else
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(_loadingSceneName, LoadSceneMode.Single);
    }

    private void OpenSettings()
    {
        if (_settingsView != null && !_settingsView.IsOpen)
        {
            _settingsView.Open();
        }
        else if (_menu != null)
        {
            _menu.SetLocked(false);
        }
    }

    private void HandleSettingsClosed()
    {
        if (_menu != null && !_isStarting)
        {
            _menu.SetLocked(false);
        }
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}