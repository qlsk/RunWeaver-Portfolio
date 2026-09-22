using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField] private float _moveSpeed = 5f;
    private Rigidbody2D _playerRigidbody2D;

    private void Start()
    {
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Move();
    }

    public void Move()
    {
        _playerRigidbody2D.linearVelocity = _playerInputHandler.MoveDirection * _moveSpeed;
    }
}
