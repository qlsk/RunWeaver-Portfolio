using UnityEngine;
using System;

public class PlayerInputHandler : MonoBehaviour
{
    private static PlayerInputHandler _instance;
    public static PlayerInputHandler Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(this);
        }

        _instance = this;
    }

    // player move key binding
    [SerializeField] private KeyCode _playerMoveUpKeyCode = KeyCode.W;
    [SerializeField] private KeyCode _playerMoveDownKeyCode = KeyCode.S;
    [SerializeField] private KeyCode _playerMoveLeftKeyCode = KeyCode.A;
    [SerializeField] private KeyCode _playerMoveRightKeyCode = KeyCode.D;

    // key binding
    [SerializeField] private KeyCode _playerOpenRuneBookKeyCode = KeyCode.Tab;
    [SerializeField] private KeyCode _playerDashKeyCode = KeyCode.Space;
    [SerializeField] private KeyCode _playerSkill1KeyCode = KeyCode.Q;
    [SerializeField] private KeyCode _playerSkill2KeyCode = KeyCode.E;

    // Event Handler
    public event EventHandler DashPressed;
    public event EventHandler Skill1Pressed;
    public event EventHandler Skill2Pressed;
    public event EventHandler OpenRuneBookPressed;

    // 마우스 position
    private Vector2 _mousePosition;
    public Vector2 MousePosition => _mousePosition;

    // player move input (위 아래 왼쪽 오른쪽)
    private Vector2 _moveDirection;
    public Vector2 MoveDirection => _moveDirection;

    private KeyCode _currentPlayerKeyCode;

    private void Update()
    {
        // Dash key 입력 시
        if (Input.GetKeyDown(_playerDashKeyCode))
        {
            // Dash 사용
            DashPressed?.Invoke(this, EventArgs.Empty);
        }

        // skill 1 key 입력 시
        if (Input.GetKeyDown(_playerSkill1KeyCode))
        {
            // 스킬 1 사용
            Skill1Pressed?.Invoke(this, EventArgs.Empty);
        }

        // skill 2 key 입력 시
        if (Input.GetKeyDown(_playerSkill2KeyCode))
        {
            // 스킬 2 사용
            Skill2Pressed?.Invoke(this, EventArgs.Empty);
        }

        // open rune book key 입력 시
        if (Input.GetKeyDown(_playerOpenRuneBookKeyCode))
        {
            // 룬 도감 오픈
            OpenRuneBookPressed?.Invoke(this, EventArgs.Empty);
        }

        // 마우스 position 갱신
        _mousePosition = Input.mousePosition;

        // player move input 갱신
        float horizontal = Input.GetAxisRaw("Horizontal"); // 좌 우
        float vertical = Input.GetAxisRaw("Vertical"); // 상 하
        _moveDirection = new Vector2(horizontal, vertical).normalized;
    }
}
