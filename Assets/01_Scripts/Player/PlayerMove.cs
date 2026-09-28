using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private Dash _dash;
    private readonly float _playerMoveSpeed = 5f;
    private PlayerInputHandler _playerInputHandler;
    private Rigidbody2D _playerRigidbody2D;

    private bool _canMove = true;
    public bool CanMove => _canMove;

    private bool _isMoving = false;
    public bool IsMoving => _isMoving;

    public void SetCanMove(bool canMove)
    {
        _canMove = canMove;
    }

    private void Start()
    {
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _dash = GetComponent<Dash>();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        if (!CanMove)
        {
            _isMoving = false;
            return;
        }

        Vector2 moveDirection = _playerInputHandler.MoveDirection;

        // 이동 상태 설정
        _isMoving = moveDirection != Vector2.zero;

        _playerRigidbody2D.linearVelocity = moveDirection * _playerMoveSpeed;
    }
}
