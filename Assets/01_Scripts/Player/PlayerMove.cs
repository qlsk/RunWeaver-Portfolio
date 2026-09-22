using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private PlayerInputHandler _playerInputHandler;
    [SerializeField] private float _moveSpeed = 3f;

    private void Start()
    {
        _playerInputHandler = FindObjectOfType<PlayerInputHandler>();
    }

    private void FixedUpdate()
    {
        Move();
    }

    public void Move()
    {
        transform.position += _moveSpeed * Time.deltaTime * (Vector3)_playerInputHandler.MoveDirection;
    }
}
