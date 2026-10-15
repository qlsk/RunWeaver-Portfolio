using UnityEngine;

/// <summary>
/// 마을의 룬북 흐름.
/// - 게이트(F) → 주 원소 선택 → Confirm : 룬북을 장착 가능한 상태로 연다. (Confirm하면 출발)
///   이 룬북에서 Esc를 누르면 주 원소 선택 단계로 돌아간다. (룬북에서 바꾼 내용은 버린다)
/// - Tab : 룬북을 보기 전용으로 연다. 룬 워드와 보유 룬 문자만 보고 장착/출발은 하지 않는다.
///   보기 전용으로 연 룬북은 Tab 또는 Esc로 닫는다.
/// </summary>
public class RuneEffectSelectionUIController : MonoBehaviour
{
    [SerializeField] private PopupController _popupController;
    [SerializeField] private MainElementSelectView _primaryElementSelectionView;
    [SerializeField] private GameObject _runeEffectActivationPopup;

    [Header("Tab (View Only)")]
    [Tooltip("비워두면 씬에서 찾는다")]
    [SerializeField] private PlayerInputHandler _inputHandler;
    [Tooltip("비워두면 룬북 팝업 안에서 찾는다")]
    [SerializeField] private RuneBookV2Presenter _runeBookPresenter;
    [Tooltip("보기 전용 룬북을 닫는 키")]
    [SerializeField] private KeyCode[] _viewOnlyCloseKeys = { KeyCode.Tab, KeyCode.Escape };

    [Header("Back To Element Select")]
    [Tooltip("장착용 룬북에서 주 원소 선택으로 돌아가는 키")]
    [SerializeField] private KeyCode _backKey = KeyCode.Escape;

    private bool _isViewOnlyOpen;
    private bool _isEquipOpen;
    private int _lastToggleFrame = -1;

    private void Awake()
    {
        if (_inputHandler == null)
        {
            _inputHandler = FindAnyObjectByType<PlayerInputHandler>();
        }

        if (_runeBookPresenter == null && _runeEffectActivationPopup != null)
        {
            _runeBookPresenter = _runeEffectActivationPopup.GetComponentInChildren<RuneBookV2Presenter>(true);
        }
    }

    private void OnEnable()
    {
        _primaryElementSelectionView.Confirmed += OnPrimaryElementConfirmed;
        _primaryElementSelectionView.Cancelled += OnPrimaryElementCancelled;

        if (_inputHandler != null)
        {
            _inputHandler.OpenRuneBookPressed += OnOpenRuneBookPressed;
        }
    }

    private void OnDisable()
    {
        _primaryElementSelectionView.Confirmed -= OnPrimaryElementConfirmed;
        _primaryElementSelectionView.Cancelled -= OnPrimaryElementCancelled;

        if (_inputHandler != null)
        {
            _inputHandler.OpenRuneBookPressed -= OnOpenRuneBookPressed;
        }
    }

    private void Update()
    {
        if (!_isViewOnlyOpen)
        {
            return;
        }

        // 다른 경로로 닫혔으면 상태만 정리한다.
        if (_runeEffectActivationPopup == null || !_runeEffectActivationPopup.activeSelf)
        {
            _isViewOnlyOpen = false;
            return;
        }

        if (_lastToggleFrame == Time.frameCount)
        {
            return;
        }

        foreach (KeyCode closeKey in _viewOnlyCloseKeys)
        {
            if (Input.GetKeyDown(closeKey))
            {
                CloseViewOnly();
                return;
            }
        }
    }

    /// <summary>
    /// 장착용 룬북에서 Esc를 누르면 주 원소 선택으로 돌아간다.
    /// 모든 Update가 끝난 뒤 바꿔서, 새로 켜진 원소 선택 창이 같은 프레임의 Esc를 취소로 읽지 않게 한다.
    /// </summary>
    private void LateUpdate()
    {
        if (!_isEquipOpen)
        {
            return;
        }

        if (_runeEffectActivationPopup == null || !_runeEffectActivationPopup.activeSelf)
        {
            _isEquipOpen = false;
            return;
        }

        if (_lastToggleFrame == Time.frameCount || !Input.GetKeyDown(_backKey))
        {
            return;
        }

        _lastToggleFrame = Time.frameCount;
        _isEquipOpen = false;
        _popupController.OpenPopup(_primaryElementSelectionView.gameObject);
    }

    private void OnPrimaryElementConfirmed(ReactionElement element)
    {
        _lastToggleFrame = Time.frameCount;
        _isEquipOpen = true;
        _isViewOnlyOpen = false;
        SetViewOnly(false);
        _popupController.OpenPopup(_runeEffectActivationPopup);
    }

    private void OnPrimaryElementCancelled()
    {
        _popupController.ClosePopup();
    }

    private void OnOpenRuneBookPressed()
    {
        // 닫은 프레임의 같은 Tab 입력으로 다시 열지 않는다. 다른 팝업이 열려 있으면 열지 않는다.
        if (_lastToggleFrame == Time.frameCount || _popupController.IsOpen || PauseController.IsGamePaused)
        {
            return;
        }

        _lastToggleFrame = Time.frameCount;
        _isEquipOpen = false;
        _isViewOnlyOpen = true;
        SetViewOnly(true);
        _popupController.OpenPopup(_runeEffectActivationPopup);
    }

    private void CloseViewOnly()
    {
        _lastToggleFrame = Time.frameCount;
        _isViewOnlyOpen = false;
        _popupController.ClosePopup();
    }

    private void SetViewOnly(bool isViewOnly)
    {
        if (_runeBookPresenter != null)
        {
            _runeBookPresenter.SetViewOnly(isViewOnly);
        }
    }
}
