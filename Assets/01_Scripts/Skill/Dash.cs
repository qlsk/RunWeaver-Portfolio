using System.Collections;
using UnityEngine;

public class Dash : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField] private float _dashDuration = 1f;
    [SerializeField] private float _dashPower = 5f;
    [SerializeField] private float _dashCooldown = 3f;
    private Rigidbody2D _playerRigidbody2D;
    private PlayerMove _playerMove;

    private bool _isDashing = false;
    private bool _canDash = true;

    private void Start()
    {
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        _playerMove = GetComponent<PlayerMove>();
    }

    private void OnEnable()
    {
        _playerInputHandler.DashPressed += DoDash;
    }

    private void OnDisable()
    {
        _playerInputHandler.DashPressed -= DoDash;
    }

    private void DoDash()
    {
        if (!_canDash || _isDashing) return;
        StartCoroutine(DashCoroutine());
    }

    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        _canDash = false;

        // 
        _playerMove.SetCanMove(false);
        Vector2 dashDirection = _playerInputHandler.MoveDirection;
        _playerRigidbody2D.linearVelocity = dashDirection * _dashPower;

        // dash 유지 시간
        yield return new WaitForSeconds(_dashDuration);

        _playerMove.SetCanMove(true);
        _isDashing = false;

        // dash 쿨타임
        yield return new WaitForSeconds(_dashCooldown);
        _canDash = true;
    }
}
