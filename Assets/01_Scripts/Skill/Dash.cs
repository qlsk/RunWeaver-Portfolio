using System.Collections;
using UnityEngine;

public class Dash : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField] private float _dashDuration = 1f;
    [SerializeField] private float _dashPower = 5f;
    private Rigidbody2D _playerRigidbody2D;

    private bool _isDashing;
    public bool IsDashing => _isDashing;

    private void Start()
    {
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
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
        StartCoroutine(DashCoroutine());
        _playerRigidbody2D.linearVelocity = _dashPower * _playerInputHandler.MoveDirection;
    }

    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        Vector2 dashDirection = _playerInputHandler.MoveDirection;
        _playerRigidbody2D.linearVelocity = dashDirection * _dashPower;

        yield return new WaitForSeconds(_dashDuration);

        _isDashing = false;
    }
}
