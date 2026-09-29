using System.Collections;
using UnityEngine;

public class Dash : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [Header("Dash 지속 시간")]
    [SerializeField] private float _dashDuration = 1f;
    [Header("Dash 파워")]
    [SerializeField] private float _dashPower = 5f;
    [Header("Dash 쿨타임")]
    [SerializeField] private float _dashCooldown = 3f;
    private Rigidbody2D _playerRigidbody2D;
    private PlayerMove _playerMove;
    private PlayerHealth _playerHealth;

    private bool _isDashing = false;
    private bool _canDash = true;

    private void Start()
    {
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        _playerMove = GetComponent<PlayerMove>();
        _playerHealth = GetComponent<PlayerHealth>();
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

        // dash 중 무적
        _playerHealth.SetCanTakeDamage(false);
        // dahs 중 조작 불가능
        _playerMove.SetCanMove(false);
        // dash 목표 지점 설정 (이동 방향)
        Vector2 dashDirection = _playerInputHandler.MoveDirection;

        // dash 수행
        _playerRigidbody2D.linearVelocity = dashDirection * _dashPower;

        // dash 유지 시간
        yield return new WaitForSeconds(_dashDuration);

        // dash 무적 끝
        _playerHealth.SetCanTakeDamage(true);
        // dash 후 조작 가능
        _playerMove.SetCanMove(true);
        _isDashing = false;

        // dash 쿨타임
        yield return new WaitForSeconds(_dashCooldown);
        _canDash = true;
    }
}
