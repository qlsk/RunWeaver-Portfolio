using System.Collections;
using UnityEngine;

[RequireComponent(typeof(DashAfterimage))]
public class Dash : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [Header("Dash 지속 시간")]
    [SerializeField] private float _dashDuration = 0.2f;
    [Header("Dash 파워")]
    [SerializeField] private float _dashPower = 10f;
    [Header("Dash 쿨타임")]
    [SerializeField] private float _dashCooldown = 3f;
    private DashAfterimage _dashAfterimage;
    private Rigidbody2D _playerRigidbody2D;
    private PlayerHealth _playerHealth;
    private PlayerStatusEffectController _statusEffects;

    private bool _isDashing = false;
    public bool IsDashing => _isDashing;
    private bool _canDash = true;

    private void Start()
    {
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        _playerHealth = GetComponent<PlayerHealth>();
        _statusEffects = GetComponent<PlayerStatusEffectController>();
        _dashAfterimage = GetComponent<DashAfterimage>();
    }

    private void OnEnable()
    {
        _playerInputHandler.DashPressed += DoDash;
    }

    private void OnDisable()
    {
        if (_playerInputHandler != null)
        {
            _playerInputHandler.DashPressed -= DoDash;
        }

        StopAllCoroutines();
        if (_dashAfterimage != null) _dashAfterimage.Clear();

        if (_playerHealth != null)
        {
            _playerHealth.SetDashInvincibility(false);
        }

        if (_isDashing && _playerRigidbody2D != null)
        {
            _playerRigidbody2D.linearVelocity = Vector2.zero;
        }

        _isDashing = false;
        _canDash = true;
    }

    private void DoDash()
    {
        if (!_canDash || _isDashing) return;
        if (_statusEffects != null && _statusEffects.IsStaggered) return;
        StartCoroutine(DashCoroutine());
    }

    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        _canDash = false;
        _dashAfterimage.BeginEmission();

        // dash 중 무적
        _playerHealth.SetDashInvincibility(true);
        
        Vector2 dashDirection;
        
        // dash 목표 지점 설정 (이동 방향)
        if (_playerInputHandler.MoveDirection == Vector2.zero)
        {
            dashDirection = _playerInputHandler.LastDirection;
        }
        else
        {
            dashDirection = _playerInputHandler.MoveDirection;
        }

        // dash 수행
        _playerRigidbody2D.linearVelocity = dashDirection * _dashPower;

        // dash 유지 시간
        yield return new WaitForSeconds(_dashDuration);

        // dash 무적 끝
        _playerHealth.SetDashInvincibility(false);
        _dashAfterimage.StopEmission();
        // dash 후 조작 가능
        _isDashing = false;

        // dash 쿨타임
        yield return new WaitForSeconds(_dashCooldown);
        _canDash = true;
    }

}
