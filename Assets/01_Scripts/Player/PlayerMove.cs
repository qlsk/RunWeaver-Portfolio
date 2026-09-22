using UnityEngine;

public class PlayerMove : MonoBehaviour
{
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
        _playerRigidbody2D.linearVelocity = PlayerInputHandler.Instance.MoveDirection * _playerMoveSpeed;
    }
}
