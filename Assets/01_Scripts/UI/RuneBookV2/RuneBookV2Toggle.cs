using UnityEngine;

/// <summary>
/// 전투 중 새 룬북(RuneBookV2)을 열고 닫는다.
/// 열기: PlayerInputHandler의 OpenRuneBookPressed (기본 Tab)
/// 닫기: 열린 동안 닫기 키(Tab, Esc) 입력. Confirm하지 않고 닫으면 바꾼 내용은 버린다. (RuneBookV2Presenter)
/// 룬북이 열려 있는 동안은 RuneBookPauseController로 게임을 멈춘다.
/// 일시정지 메뉴가 열려 있거나 막는 화면(결과 화면 등)이 켜져 있으면 열지 않는다.
/// 룬북은 꺼진 상태로 시작하므로 항상 켜져 있는 오브젝트에 붙인다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookV2Toggle : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _inputHandler;
    [Tooltip("룬북 화면 전체 (RuneBookV2 프리팹 루트)")]
    [SerializeField] private GameObject _runeBook;
    [SerializeField] private RuneBookPauseController _pauseController;

    [Tooltip("룬북이 열려 있을 때 닫는 키. 일시정지 중에는 PlayerInputHandler가 꺼지므로 직접 읽는다.")]
    [SerializeField] private KeyCode[] _closeKeys = { KeyCode.Tab, KeyCode.Escape };

    [Tooltip("이 중 하나라도 켜져 있으면 룬북을 열 수 없다 (결과 화면 등)")]
    [SerializeField] private GameObject[] _blockers;

    private int _lastToggleFrame = -1;

    public bool IsOpen => _runeBook != null && _runeBook.activeSelf;

    private void Awake()
    {
        if (_runeBook == null)
        {
            Debug.LogError("[RuneBookV2Toggle] 룬북 오브젝트가 할당되지 않았습니다.", this);
            return;
        }

        // 씬에 켜진 상태로 저장되어 있어도 게임은 룬북이 닫힌 상태로 시작한다.
        _runeBook.SetActive(false);
    }

    private void OnEnable()
    {
        if (_inputHandler == null)
        {
            Debug.LogError("[RuneBookV2Toggle] PlayerInputHandler가 할당되지 않았습니다.", this);
            return;
        }

        _inputHandler.OpenRuneBookPressed += HandleOpenRuneBookPressed;
    }

    private void OnDisable()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OpenRuneBookPressed -= HandleOpenRuneBookPressed;
        }
    }

    private void Update()
    {
        if (!IsOpen || _lastToggleFrame == Time.frameCount)
        {
            return;
        }

        foreach (KeyCode closeKey in _closeKeys)
        {
            if (Input.GetKeyDown(closeKey))
            {
                Close();
                return;
            }
        }
    }

    public void Open()
    {
        if (_runeBook == null || IsOpen || IsBlocked())
        {
            return;
        }

        _lastToggleFrame = Time.frameCount;
        _runeBook.SetActive(true);

        if (_pauseController != null)
        {
            _pauseController.Pause();
        }
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        // 닫은 프레임에 다시 켜진 PlayerInputHandler가 같은 Tab 입력으로 룬북을 다시 열지 않도록 기록한다.
        _lastToggleFrame = Time.frameCount;
        _runeBook.SetActive(false);

        if (_pauseController != null)
        {
            _pauseController.Resume();
        }
    }

    private bool IsBlocked()
    {
        if (PauseController.IsGamePaused)
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

    private void HandleOpenRuneBookPressed()
    {
        if (_lastToggleFrame == Time.frameCount)
        {
            return;
        }

        Open();
    }
}
