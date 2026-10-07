using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 타이틀(StartScene) 메뉴 버튼을 연결한다.
/// - New Game : 새 게임을 시작하고 로딩 화면(LoadingScene)을 거쳐 마을로 간다.
/// - Continue Game : 저장 기능이 없어 지금은 누를 수 없게 둔다.
/// - Setting : 설정 창(SettingsUI)을 연다.
/// - Quit Game : 게임을 끝낸다.
/// 버튼 오브젝트는 버튼 프리팹의 루트를 넣는다. (Button이 자식에 있어도 찾는다)
/// </summary>
[DisallowMultipleComponent]
public class TitleMenuController : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private GameObject _newGameButton;
    [SerializeField] private GameObject _continueButton;
    [SerializeField] private GameObject _settingsButton;
    [SerializeField] private GameObject _quitButton;

    [Tooltip("시작 버튼을 누르면 갈 로딩 씬. 로딩 씬이 데이터를 불러온 뒤 마을로 넘어간다.")]
    [SerializeField] private string _loadingSceneName = "LoadingScene";

    [Header("설정 창 (SettingsUI 루트)")]
    [SerializeField] private GameObject _settingsPanel;

    private Button _newGame;
    private Button _continue;
    private Button _settings;
    private Button _quit;
    private SettingsPanelView _settingsView;
    private bool _isStarting;

    private void Awake()
    {
        _newGame = FindButton(_newGameButton);
        _continue = FindButton(_continueButton);
        _settings = FindButton(_settingsButton);
        _quit = FindButton(_quitButton);
        _settingsView = _settingsPanel != null ? _settingsPanel.GetComponentInChildren<SettingsPanelView>(true) : null;

        if (_newGame != null) _newGame.onClick.AddListener(StartNewGame);
        if (_settings != null) _settings.onClick.AddListener(OpenSettings);
        if (_quit != null) _quit.onClick.AddListener(QuitGame);

        // TODO: 저장 기능이 생기면 이어하기를 연결한다.
        if (_continue != null) _continue.interactable = false;
    }

    private void OnDestroy()
    {
        if (_newGame != null) _newGame.onClick.RemoveListener(StartNewGame);
        if (_settings != null) _settings.onClick.RemoveListener(OpenSettings);
        if (_quit != null) _quit.onClick.RemoveListener(QuitGame);
    }

    private void StartNewGame()
    {
        if (_isStarting)
        {
            return;
        }

        _isStarting = true;
        GameSession session = GameSession.Instance;
        if (session != null && session.IsReady && !session.Current.Holdings.IsOnExpedition)
        {
            session.StartNewGame();
        }

        OwnedRuneWords.Clear();
        SceneManager.LoadScene(_loadingSceneName, LoadSceneMode.Single);
    }

    private void OpenSettings()
    {
        if (_settingsView != null && !_settingsView.IsOpen)
        {
            _settingsView.Open();
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

    private static Button FindButton(GameObject target)
    {
        return target != null ? target.GetComponentInChildren<Button>(true) : null;
    }
}
