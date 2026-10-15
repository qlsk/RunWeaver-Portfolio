using System;
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

    /// <summary>보상 특성용: 대시 시작/종료 통지. 종료는 이동 끝 시점(쿨타임 전)에 나간다.</summary>
    public event Action DashStarted;
    public event Action DashFinished;

    /// <summary>보상 특성용 배율 (기본 1). 대시 거리·쿨타임에 곱해진다.</summary>
    public float DashPowerMultiplier { get; set; } = 1f;
    public float DashCooldownMultiplier { get; set; } = 1f;
    private float _cooldownEndTime;
    private float _cooldownTotal;

    /// <summary>HUD용: 다시 대시할 수 있을 때까지 남은 시간(초). 쓸 수 있으면 0</summary>
    public float CooldownRemaining => _canDash ? 0f : Mathf.Max(0f, _cooldownEndTime - Time.time);

    /// <summary>HUD용: 대시를 쓴 뒤 다시 쓸 수 있을 때까지 걸리는 전체 시간(초) (지속 시간 + 쿨타임)</summary>
    // 쿨타임 감소(DashCooldownMultiplier)가 적용된 값이다. (대시를 쓸 때 정한 값을 그대로 쓴다)
    public float CooldownDuration => _cooldownTotal > 0f
        ? _cooldownTotal
        : _dashDuration + _dashCooldown * DashCooldownMultiplier;

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
        DashStarted?.Invoke();
        StartCoroutine(DashCoroutine());
    }

    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        _canDash = false;
        _cooldownTotal = _dashDuration + _dashCooldown * DashCooldownMultiplier;
        _cooldownEndTime = Time.time + _cooldownTotal;
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
        _playerRigidbody2D.linearVelocity = dashDirection * (_dashPower * DashPowerMultiplier);

        // dash 유지 시간
        yield return new WaitForSeconds(_dashDuration);

        // dash 무적 끝
        _playerHealth.SetDashInvincibility(false);
        _dashAfterimage.StopEmission();
        // dash 후 조작 가능
        _isDashing = false;
        DashFinished?.Invoke();

        // dash 쿨타임 (시작할 때 정한 시간 그대로 기다린다)
        yield return new WaitForSeconds(Mathf.Max(0f, _cooldownTotal - _dashDuration));
        _canDash = true;
    }

}
