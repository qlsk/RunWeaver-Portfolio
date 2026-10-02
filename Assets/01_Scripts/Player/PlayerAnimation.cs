using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private PlayerInputHandler _playerInputHandler;
    private PlayerArm _arm;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _arm = GetComponent<PlayerArm>();
    }

    // mouseposition.x - transform.position.x > 0 이면 오른쪽 <0 이면 왼쪽 flip true : false
    private void Update()
    {
        Vector2 mousePosition = _playerInputHandler.MousePosition;
        _animator.SetBool("isRunning", _playerInputHandler.MoveDirection != Vector2.zero);

        _spriteRenderer.flipX = mousePosition.x - transform.position.x > 0 ? true : false;
    }
}