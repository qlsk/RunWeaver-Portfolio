using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private PlayerInputHandler _playerInputHandler;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _playerInputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        _animator.SetBool("isRunning", _playerInputHandler.MoveDirection != Vector2.zero);
        if (_playerInputHandler.MoveDirection.x > 0f)
        {
            _spriteRenderer.flipX = true;
        }
        else if (_playerInputHandler.MoveDirection.x < 0f)
        {
            _spriteRenderer.flipX = false;
        }
    }
}