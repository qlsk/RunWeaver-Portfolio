using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// 타이틀(StartScene) 메뉴를 연결한다. 메뉴 항목 순서는 아래와 같다.
/// - 0 새 게임 : 새 게임을 시작하고, 시작 연출(TitleSequence)이 끝나면 로딩 화면(LoadingScene)을 거쳐 전투 씬으로 간다.
/// - 1 이어하기 : 진행 중인 판이 저장되어 있을 때만 누를 수 있다. 저장된 층부터 이어서 한다. (SaveSystem, RunSaveController)
///   이어할 판이 있으면 처음부터 이어하기가 선택되어 있고, 옆에 저장된 층을 보여준다.
///   이어할 판이 있을 때 새 게임을 고르면 확인 창(ConfirmDialogView)을 띄운다. 확인하면 저장된 판을 지우고 시작한다.
///   확인 창에서는 방향키로 고르고 F / Enter로 결정, Esc는 취소와 같다.
/// - 2 설정 : 설정 창(SettingsUI)을 연다. 창이 열린 동안 메뉴는 잠기고, Esc는 취소와 같다.
/// - 3 게임 종료 : 게임을 끝낸다.
/// </summary>
[DisallowMultipleComponent]
public class TitleMenuController : MonoBehaviour
{
    private const int NewGameIndex = 0;
    private const int ContinueIndex = 1;
    private const int SettingsIndex = 2;
    private const int QuitIndex = 3;

    [SerializeField] private TitleMenuView _menu;
    [SerializeField] private TitleSequence _sequence;

    [Tooltip("시작 버튼을 누르면 갈 로딩 씬. 로딩 씬이 데이터를 불러온 뒤 전투 씬으로 넘어간다.")]
    [SerializeField] private string _loadingSceneName = "LoadingScene";

    [Header("이어하기")]
    [Tooltip("이어하기 옆 작은 글자. {0} 저장된 층")]
    [SerializeField] private string _continueSubTextFormat = "{0}층";

    [Header("새 게임 확인 창 (이어할 판이 있을 때)")]
    [SerializeField] private ConfirmDialogView _newGameConfirm;
    [SerializeField] private string _newGameConfirmTitle = "새 게임을 시작할까요?";
    [Tooltip("{0} 저장된 층")]
    [SerializeField] private string _newGameConfirmBody = "진행 중인 탐사 기록({0}층)이 지워집니다.";
    [SerializeField] private string _newGameConfirmLabel = "새 게임";

    [Header("설정 창 (SettingsUI 루트)")]
    [SerializeField] private GameObject _settingsPanel;

    [Header("BGM")]
    [SerializeField] private TitleBgm _bgm;

    private SettingsPanelView _settingsView;
    private bool _isStarting;
    private int _confirmOpenedFrame = -1;

    private bool IsConfirmOpen => _newGameConfirm != null && _newGameConfirm.IsOpen;

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

    private void Start()
    {
        if (_newGameConfirm != null)
        {
            _newGameConfirm.Hide();
        }

        ApplyContinueState();
    }

    private void Update()
    {
        // 확인 창 : Esc는 취소, F는 선택한 버튼 결정. (Enter, Space, 방향키는 EventSystem이 처리한다)
        // 창을 연 프레임에는 받지 않는다. (메뉴를 고른 키가 창의 버튼까지 누르지 않게)
        if (IsConfirmOpen && Time.frameCount != _confirmOpenedFrame)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _newGameConfirm.Cancel();
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
                SubmitSelected();
            }

            return;
        }

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
                if (SaveSystem.HasRun && _newGameConfirm != null)
                {
                    // 메뉴는 잠근 채로 두고, 다음 프레임에 확인 창을 연다. (메뉴를 고른 키 입력이 창의 버튼에 닿지 않게)
                    StartCoroutine(OpenNewGameConfirm());
                    break;
                }

                StartNewGame();
                break;
            case ContinueIndex:
                ContinueGame();
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

        SaveSystem.DeleteRun();
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

    /// <summary>이어하기 : 로딩 화면을 거쳐 전투 씬으로 가고, 전투 씬이 저장된 층부터 시작한다. (RunSaveController)</summary>
    private void ContinueGame()
    {
        if (_isStarting || !SaveSystem.HasRun)
        {
            if (_menu != null) _menu.SetLocked(false);
            return;
        }

        _isStarting = true;

        // BGM 페이드아웃을 요청한다.
        _bgm.FadeOut();

        SaveSystem.RequestContinue();

        if (_sequence != null)
        {
            _sequence.PlayStart(ContinueIndex, LoadNextScene);
        }
        else
        {
            LoadNextScene();
        }
    }

    /// <summary>이어할 판이 있으면 이어하기를 켜고 저장된 층을 보여준 뒤 먼저 선택한다.</summary>
    private void ApplyContinueState()
    {
        RuneMenuItemView continueItem = _menu != null ? _menu.GetItem(ContinueIndex) : null;
        if (continueItem == null)
        {
            return;
        }

        RunSaveData run = SaveSystem.Run;
        continueItem.SetDisabled(run == null);
        if (run != null)
        {
            continueItem.SetSubText(string.Format(_continueSubTextFormat, run.Depth));
            _menu.SelectItem(ContinueIndex);
        }
    }

    private IEnumerator OpenNewGameConfirm()
    {
        yield return null;

        RunSaveData run = SaveSystem.Run;
        if (_isStarting || run == null)
        {
            if (_menu != null) _menu.SetLocked(false);
            yield break;
        }

        _confirmOpenedFrame = Time.frameCount;
        _newGameConfirm.Show(_newGameConfirmTitle, string.Format(_newGameConfirmBody, run.Depth), _newGameConfirmLabel,
            StartNewGame, HandleNewGameCancelled);
    }

    private void HandleNewGameCancelled()
    {
        if (_menu != null && !_isStarting)
        {
            _menu.SetLocked(false);
            _menu.SelectItem(NewGameIndex);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>F 키로 확인 창에서 선택한 버튼을 누른다.</summary>
    private static void SubmitSelected()
    {
        EventSystem eventSystem = EventSystem.current;
        GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
        if (selected == null)
        {
            return;
        }

        ExecuteEvents.Execute(selected, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
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