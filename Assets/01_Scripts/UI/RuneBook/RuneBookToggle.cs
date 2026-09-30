using UnityEngine;

/// <summary>
/// 룬북 열기/닫기 입력을 처리한다.
/// 열기: PlayerInputHandler의 OpenRuneBookPressed (기본 Tab)
/// 닫기: 열린 동안 닫기 키(Tab, Esc) 입력
/// 룬북이 열려 있는 동안은 RuneBookPauseController로 게임을 멈춘다.
/// 룬북 패널은 꺼진 상태로 시작할 수 있으므로 항상 켜져 있는 오브젝트에 붙인다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookToggle : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private RuneBookPresenter _presenter;
    [SerializeField] private RuneBookPauseController _pauseController;

    [Tooltip("룬북이 열려 있을 때 닫는 키. 일시정지 중에는 PlayerInputHandler가 꺼지므로 직접 읽는다.")]
    [SerializeField] private KeyCode[] _closeKeys = { KeyCode.Tab, KeyCode.Escape };

    private int _lastToggleFrame = -1;

    private void Awake()
    {
        if (_presenter == null)
        {
            Debug.LogError("[RuneBookToggle] RuneBookPresenter가 할당되지 않았습니다.", this);
            return;
        }

        // 씬에 켜진 상태로 저장되어 있어도 게임은 룬북이 닫힌 상태로 시작한다.
        _presenter.Close();
    }

    private void OnEnable()
    {
        if (_inputHandler == null)
        {
            Debug.LogError("[RuneBookToggle] PlayerInputHandler가 할당되지 않았습니다.", this);
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
        if (_presenter == null || !_presenter.IsOpen || _lastToggleFrame == Time.frameCount)
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
        if (_presenter == null || _presenter.IsOpen)
        {
            return;
        }

        _lastToggleFrame = Time.frameCount;
        _presenter.Open();

        if (_pauseController != null)
        {
            _pauseController.Pause();
        }
    }

    public void Close()
    {
        if (_presenter == null || !_presenter.IsOpen)
        {
            return;
        }

        // 닫은 프레임에 다시 켜진 PlayerInputHandler가 같은 Tab 입력으로 룬북을 다시 열지 않도록 기록한다.
        _lastToggleFrame = Time.frameCount;
        _presenter.Close();

        if (_pauseController != null)
        {
            _pauseController.Resume();
        }
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
