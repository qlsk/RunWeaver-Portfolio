using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private Dash _dash;
    [SerializeField] private float _playerMoveSpeed = 5f;
    private PlayerInputHandler _playerInputHandler;
    private Rigidbody2D _playerRigidbody2D;

    private void Start()
    {
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        _playerInputHandler = GetComponent<PlayerInputHandler>();
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
        _playerRigidbody2D.linearVelocity = _playerInputHandler.MoveDirection * _playerMoveSpeed;
    }
}
