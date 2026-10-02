using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    // private static readonly int s_isRunning = Animator.StringToHash("isRunning");

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

    private void Update()
    {
        Vector2 mousePosition = _playerInputHandler.MousePosition;
        // _animator.SetBool(s_isRunning, _playerInputHandler.MoveDirection != Vector2.zero);

        // 기본 스프라이트가 왼쪽을 바라보고 있으므로, 마우스가 플레이어 기준 오른쪽에 위치하면 스프라이트를 뒤집는다.
        bool isMouseOnPlayerRight = mousePosition.x - transform.position.x > 0;
        _spriteRenderer.flipX = isMouseOnPlayerRight;
    }
}