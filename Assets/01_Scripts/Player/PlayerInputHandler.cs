using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PopupController _popupController;

    // key binding
    [Header("룬 도감 오픈 키")]
    [SerializeField] private KeyCode _playerOpenRuneBookKeyCode = KeyCode.Tab;
    [Header("대시 키")]
    [SerializeField] private KeyCode _playerDashKeyCode = KeyCode.Space;
    [Header("스킬 1 키")]
    [SerializeField] private KeyCode _playerSkill1KeyCode = KeyCode.Q;
    [Header("스킬 2 키")]
    [SerializeField] private KeyCode _playerSkill2KeyCode = KeyCode.E;
    [Header("재장전 키")]
    [SerializeField] private KeyCode _playerReloadKeyCode = KeyCode.R;

    // Event Handler
    public event Action DashPressed;
    public event Action Skill1Pressed;
    public event Action Skill2Pressed;
    public event Action OpenRuneBookPressed;
    public event Action InteractionRequested;
    public event Action UICloseRequested;

    // 마우스 position
    private Vector2 _mousePosition;
    public Vector2 MousePosition => _mousePosition;

    // 마우스 왼쪽 버튼을 누르고 있는지 확인
    // 팝업(상점 등)이 열려 있거나, 일시정지 중이거나, 런이 끝났으면(결과창) 사격/재장전 입력을 막는다.
    public bool IsAttackHeld => CanUseWeaponInput && Input.GetMouseButton(0);

    public bool IsReloadPressed => CanUseWeaponInput && Input.GetKeyDown(_playerReloadKeyCode);

    private bool CanUseWeaponInput => !_isPopupOpened && Time.frameCount != _popupClosedFrame
        && !PauseController.IsGamePaused && !RunFlow.HasEnded && s_cutsceneLockCount == 0;

    // 연출(보스 등장 등) 동안 이동·사격·스킬 입력을 막는다. 잠근 쪽이 반드시 풀어야 한다.
    private static int s_cutsceneLockCount;

    public static bool IsCutsceneLocked => s_cutsceneLockCount > 0;

    public static void LockForCutscene()
    {
        s_cutsceneLockCount++;
    }

    public static void UnlockForCutscene()
    {
        s_cutsceneLockCount = Mathf.Max(0, s_cutsceneLockCount - 1);
    }

    // 도메인 리로드를 끈 에디터에서도 플레이를 새로 시작하면 잠금을 지운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCutsceneLock()
    {
        s_cutsceneLockCount = 0;
    }

    // player move input (위 아래 왼쪽 오른쪽)
    private Vector2 _moveDirection;
    public Vector2 MoveDirection => _moveDirection;

    private KeyCode _currentPlayerKeyCode;

    private Vector2 _lastDirection = new Vector2(1f, 0f);
    public Vector2 LastDirection => _lastDirection;

    private bool _isPopupOpened;
    private int _popupClosedFrame = -1;

    private void OnEnable()
    {
        _isPopupOpened = _popupController.IsOpen;
        _popupController.PopupOpenStateChanged += OnPopupOpenStateChanged;
    }

    private void OnDisable()
    {
        _popupController.PopupOpenStateChanged -= OnPopupOpenStateChanged;
    }

    private void Update()
    {
        if (CanUseWeaponInput)
        {
            // 게임 입력이 허용될 때만 마우스 position 갱신
            _mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // player move input 갱신
            float horizontal = Input.GetAxisRaw("Horizontal"); // 좌 우
            float vertical = Input.GetAxisRaw("Vertical"); // 상 하
            _moveDirection = new Vector2(horizontal, vertical).normalized;
            if (_moveDirection != Vector2.zero)
                _lastDirection = _moveDirection;

            // Dash key 입력 시
            if (Input.GetKeyDown(_playerDashKeyCode))
            {
                DashPressed?.Invoke();
            }

            // skill 1 key 입력 시
            if (Input.GetKeyDown(_playerSkill1KeyCode))
            {
                Skill1Pressed?.Invoke();
            }

            // skill 2 key 입력 시
            if (Input.GetKeyDown(_playerSkill2KeyCode))
            {
                Skill2Pressed?.Invoke();
            }

            // open rune book key 입력 시
            if (Input.GetKeyDown(_playerOpenRuneBookKeyCode))
            {
                OpenRuneBookPressed?.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                InteractionRequested?.Invoke();
            }
        }
        else
        {
            _moveDirection = Vector2.zero;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UICloseRequested?.Invoke();
            }
        }
    }

    private void OnPopupOpenStateChanged(bool isOpened)
    {
        _isPopupOpened = isOpened;
        if (!isOpened)
        {
            _popupClosedFrame = Time.frameCount;
        }
    }
}
