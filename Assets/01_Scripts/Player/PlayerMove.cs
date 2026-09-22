using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField] private Dash _dash;
    [SerializeField] private float _playerMoveSpeed = 5f;
    private Rigidbody2D _playerRigidbody2D;

    private void Start()
    {
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
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
