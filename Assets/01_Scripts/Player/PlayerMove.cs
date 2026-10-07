using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private Dash _dash;
    private PlayerStatusEffectController _statusEffects;
    private readonly float _playerMoveSpeed = 5f;
    private PlayerInputHandler _playerInputHandler;
    private Rigidbody2D _playerRigidbody2D;

    /// <summary>보상 특성용 이동속도 배율 (기본 1).</summary>
    public float TraitMoveSpeedMultiplier { get; set; } = 1f;

    public bool IsMoving => _playerRigidbody2D != null && _playerRigidbody2D.linearVelocity.sqrMagnitude > 0.0001f;

    private void Start()
    {
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _dash = GetComponent<Dash>();
        _statusEffects = GetComponent<PlayerStatusEffectController>();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        if (_statusEffects != null && _statusEffects.IsStaggered)
        {
            _playerRigidbody2D.linearVelocity = Vector2.zero;
            return;
        }

        if (_dash != null && _dash.IsDashing)
        {
            return;
        }

        Vector2 moveDirection = _playerInputHandler.MoveDirection;

        float multiplier = _statusEffects != null ? _statusEffects.MoveSpeedMultiplier : 1f;
        _playerRigidbody2D.linearVelocity = moveDirection * (_playerMoveSpeed * multiplier * TraitMoveSpeedMultiplier);
    }
}
