using UnityEngine;
using System;

public class PlayerInputHandler : MonoBehaviour
{
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

    // 마우스 position
    private Vector2 _mousePosition;
    public Vector2 MousePosition => _mousePosition;

    // 마우스 왼쪽 버튼을 누르고 있는지 확인
    public bool IsAttackHeld => Input.GetMouseButton(0);

    public bool IsReloadPressed => Input.GetKeyDown(_playerReloadKeyCode);

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
            DashPressed?.Invoke();
        }

        // skill 1 key 입력 시
        if (Input.GetKeyDown(_playerSkill1KeyCode))
        {
            // 스킬 1 사용
            Skill1Pressed?.Invoke();
        }

        // skill 2 key 입력 시
        if (Input.GetKeyDown(_playerSkill2KeyCode))
        {
            // 스킬 2 사용
            Skill2Pressed?.Invoke();
        }

        // open rune book key 입력 시
        if (Input.GetKeyDown(_playerOpenRuneBookKeyCode))
        {
            // 룬 도감 오픈
            OpenRuneBookPressed?.Invoke();
        }

        // 마우스 position 갱신
        _mousePosition = Input.mousePosition;

        // player move input 갱신
        float horizontal = Input.GetAxisRaw("Horizontal"); // 좌 우
        float vertical = Input.GetAxisRaw("Vertical"); // 상 하
        _moveDirection = new Vector2(horizontal, vertical).normalized;
    }
}
