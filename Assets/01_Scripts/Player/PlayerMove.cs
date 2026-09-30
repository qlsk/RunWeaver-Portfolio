using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private Dash _dash;
    private readonly float _playerMoveSpeed = 5f;
    private PlayerInputHandler _playerInputHandler;
    private Rigidbody2D _playerRigidbody2D;

    public bool IsMoving => _playerRigidbody2D != null && _playerRigidbody2D.linearVelocity.sqrMagnitude > 0.0001f;

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
        if (_dash.IsDashing)
        {
            return;
        }

        Vector2 moveDirection = _playerInputHandler.MoveDirection;

        _playerRigidbody2D.linearVelocity = moveDirection * _playerMoveSpeed;
    }
}
