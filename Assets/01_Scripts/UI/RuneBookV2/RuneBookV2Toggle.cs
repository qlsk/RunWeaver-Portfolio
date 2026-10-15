using System.Collections;
using UnityEngine;

/// <summary>
/// 전투 중 새 룬북(RuneBookV2)을 열고 닫는다.
/// 열기: PlayerInputHandler의 OpenRuneBookPressed (기본 Tab)
/// 닫기: 열린 동안 닫기 키(Tab, Esc) 입력. Confirm하지 않고 닫으면 바꾼 내용은 버린다. (RuneBookV2Presenter)
/// 확정 확인 창(RuneBookConfirmDialog)이 열려 있는 동안에는 닫지 않는다.
/// IsCloseLocked가 켜져 있으면 닫기 키로 닫을 수 없고 확정해야만 닫힌다. (시작 석상의 첫 보조 룬 워드 선택)
/// OpenViewOnly : 보상 선택 중 Tab으로 보기 전용 룬북을 연다. 닫으면 보기 전용을 푼다.
/// 확정 확인 창에서 확정하면(RuneBookV2Presenter.Confirmed) 확정 연출을 잠깐 보여준 뒤 룬북을 닫는다.
/// 룬북이 열려 있는 동안은 RuneBookPauseController로 게임을 멈춘다.
/// 닫을 때는 RuneBookV2Fx의 닫기 연출(아래로 가라앉으며 사라짐)이 끝난 뒤 끄고 게임을 다시 움직인다.
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

    [Header("확정 후 닫기")]
    [Tooltip("확정하면 룬북을 닫는다")]
    [SerializeField] private bool _closeOnConfirm = true;
    [Tooltip("확정 연출(장착 칸이 차례로 빛남)을 보여준 뒤 닫기까지 (실제 시간, 룬북이 열린 동안 게임이 멈춘다)")]
    [SerializeField, Min(0f)] private float _closeDelayAfterConfirm = 0.55f;

    private int _lastToggleFrame = -1;
    private Coroutine _closeRoutine;
    private Coroutine _confirmCloseRoutine;
    private RuneBookV2Presenter _presenter;

    public bool IsOpen => _runeBook != null && _runeBook.activeSelf;

    /// <summary>
    /// 켜져 있으면 닫기 키(Tab, Esc)로 룬북을 닫지 못한다. 확정하면 평소처럼 닫힌다.
    /// 코드에서 부르는 Close()는 그대로 동작한다. 켠 쪽이 끝나면 다시 꺼야 한다.
    /// </summary>
    public bool IsCloseLocked { get; set; }

    /// <summary>이번 프레임에 열거나 닫았는지 (같은 Tab 입력으로 다른 곳에서 다시 열지 않게)</summary>
    public bool WasToggledThisFrame => _lastToggleFrame == Time.frameCount;

    private bool _isViewOnlyOpen;

    /// <summary>닫기 연출 중인지</summary>
    public bool IsClosing => _closeRoutine != null;

    private void Awake()
    {
        if (_runeBook == null)
        {
            Debug.LogError("[RuneBookV2Toggle] 룬북 오브젝트가 할당되지 않았습니다.", this);
            return;
        }

        // 씬에 켜진 상태로 저장되어 있어도 게임은 룬북이 닫힌 상태로 시작한다.
        _runeBook.SetActive(false);

        // 화면 오른쪽 위 메뉴 안내 ([룬북][TAB] [일시정지][ESC], 새로 장착 가능한 룬 워드 알림)
        HudMenuHints.Create(this);
    }

    private void OnEnable()
    {
        if (_inputHandler == null)
        {
            Debug.LogError("[RuneBookV2Toggle] PlayerInputHandler가 할당되지 않았습니다.", this);
            return;
        }

        _inputHandler.OpenRuneBookPressed += HandleOpenRuneBookPressed;

        if (_runeBook != null)
        {
            _presenter = _runeBook.GetComponentInChildren<RuneBookV2Presenter>(true);
            if (_presenter != null)
            {
                _presenter.Confirmed += HandleConfirmed;
                _presenter.ViewOnlyCloseRequested += Close;
            }
        }
    }

    private void Update()
    {
        if (!IsOpen || IsClosing || _lastToggleFrame == Time.frameCount)
        {
            return;
        }

        // 확정 확인 창이 열려 있거나 방금 닫힌 프레임에는 Esc를 확인 창이 쓴다.
        if (RuneBookConfirmDialog.IsOpen || RuneBookConfirmDialog.ClosedFrame == Time.frameCount)
        {
            return;
        }

        if (IsCloseLocked)
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
        if (_runeBook == null || IsOpen || IsClosing || IsBlocked())
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

    /// <summary>
    /// 보기 전용으로 연다. (보상 선택 중 Tab) 장착을 바꾸거나 확정할 수 없고, 닫으면 보기 전용을 푼다.
    /// 열지 못하면(막는 화면이 있는 등) 아무것도 바꾸지 않는다.
    /// </summary>
    public void OpenViewOnly()
    {
        if (_presenter == null || IsOpen || IsClosing || IsBlocked())
        {
            return;
        }

        _presenter.SetViewOnly(true);
        _isViewOnlyOpen = true;
        Open();
    }

    /// <summary>오른쪽 위 메뉴 안내의 룬북 표시를 한 번 빛낸다. (룬석 보상 카드를 골랐을 때)</summary>
    public void PulseHint()
    {
        HudMenuHints hints = GetComponent<HudMenuHints>();
        if (hints != null)
        {
            hints.PulseRuneBook();
        }
    }

    public void Close()
    {
        if (!IsOpen || IsClosing)
        {
            return;
        }

        _lastToggleFrame = Time.frameCount;
        RuneBookV2Fx fx = _runeBook.GetComponent<RuneBookV2Fx>();
        if (fx == null || !isActiveAndEnabled)
        {
            FinishClose();
            return;
        }

        _closeRoutine = StartCoroutine(CloseRoutine(fx));
    }

    private IEnumerator CloseRoutine(RuneBookV2Fx fx)
    {
        yield return fx.PlayClose();
        _closeRoutine = null;
        FinishClose();
    }

    private void FinishClose()
    {
        // 닫은 프레임에 다시 켜진 PlayerInputHandler가 같은 Tab 입력으로 룬북을 다시 열지 않도록 기록한다.
        _lastToggleFrame = Time.frameCount;
        _runeBook.SetActive(false);

        if (_isViewOnlyOpen)
        {
            _isViewOnlyOpen = false;
            if (_presenter != null)
            {
                _presenter.SetViewOnly(false);
            }
        }

        if (_pauseController != null)
        {
            _pauseController.Resume();
        }
    }

    private void OnDisable()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OpenRuneBookPressed -= HandleOpenRuneBookPressed;
        }

        if (_presenter != null)
        {
            _presenter.Confirmed -= HandleConfirmed;
            _presenter.ViewOnlyCloseRequested -= Close;
            _presenter = null;
        }

        if (_confirmCloseRoutine != null)
        {
            StopCoroutine(_confirmCloseRoutine);
            _confirmCloseRoutine = null;
        }

        // 닫는 도중 토글이 꺼지면 바로 마무리한다.
        if (_closeRoutine != null)
        {
            _closeRoutine = null;
            if (_runeBook != null && _runeBook.activeSelf)
            {
                FinishClose();
            }
        }
    }

    private bool IsBlocked()
    {
        if (PauseController.IsGamePaused)
        {
            return true;
        }

        // 층 전환으로 화면을 덮는 중에는 열지 않는다. (걷히는 중에는 열 수 있고, 검은 화면은 잠시 숨는다)
        if (ScreenFader.IsCovering)
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

    private void HandleConfirmed()
    {
        if (!_closeOnConfirm || !IsOpen || IsClosing || !isActiveAndEnabled)
        {
            return;
        }

        if (_confirmCloseRoutine != null)
        {
            StopCoroutine(_confirmCloseRoutine);
        }

        _confirmCloseRoutine = StartCoroutine(CloseAfterConfirmRoutine());
    }

    private IEnumerator CloseAfterConfirmRoutine()
    {
        yield return new WaitForSecondsRealtime(_closeDelayAfterConfirm);
        _confirmCloseRoutine = null;
        Close();
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
