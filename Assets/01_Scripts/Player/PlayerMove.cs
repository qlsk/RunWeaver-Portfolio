using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("배경 경계")]
    [SerializeField] private CameraFollow _boundaryCamera;
    [SerializeField] private SpriteRenderer _boundarySprite;
    [SerializeField] private float _topBoundaryPadding = 0.15f;
    private Collider2D _playerCollider;
    private Dash _dash;
    private PlayerStatusEffectController _statusEffects;
    private readonly float _playerMoveSpeed = 5f;
    private PlayerInputHandler _playerInputHandler;
    private Rigidbody2D _playerRigidbody2D;
    private float _stepTimer;
    private const float StrideLength = 1.75f;
    private const float MinStepInterval = 0.22f;
    private const float MaxStepInterval = 0.6f;

    /// <summary>보상 특성용 이동속도 배율 (기본 1).</summary>
    public float TraitMoveSpeedMultiplier { get; set; } = 1f;

    public bool IsMoving => _playerRigidbody2D != null && _playerRigidbody2D.linearVelocity.sqrMagnitude > 0.0001f;

    private void Start()
    {
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _dash = GetComponent<Dash>();
        _statusEffects = GetComponent<PlayerStatusEffectController>();
        _playerCollider = GetComponent<Collider2D>();
        if (_boundarySprite == null)
            _boundarySprite = GetComponent<SpriteRenderer>();
        if (_boundaryCamera == null && Camera.main != null)
            _boundaryCamera = Camera.main.GetComponentInParent<CameraFollow>();
    }

    private void FixedUpdate()
    {
        Move();
        ClampToBackground(true);
    }

    private void LateUpdate()
    {
        // 충돌 등 물리 처리로 밀려난 위치도 카메라 추적 전에 보정한다.
        ClampToBackground(false);
    }

    private void ClampToBackground(bool limitVelocity)
    {
        if (_playerRigidbody2D == null || _boundaryCamera == null ||
            !_boundaryCamera.TryGetBackgroundBounds(out Bounds bounds) ||
            bounds.size.x <= 0f || bounds.size.y <= 0f)
            return;

        Vector2 position = _playerRigidbody2D.position;
        Vector2 extents = Vector2.zero;
        Vector2 offset = Vector2.zero;
        Bounds playerBounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        if (_playerCollider != null && _playerCollider.enabled)
        {
            playerBounds = _playerCollider.bounds;
            hasBounds = true;
        }
        // 발 주변의 콜라이더만 사용하면 머리가 배경 밖으로 나갈 수 있다.
        if (_boundarySprite != null && _boundarySprite.enabled && _boundarySprite.sprite != null)
        {
            if (hasBounds) playerBounds.Encapsulate(_boundarySprite.bounds);
            else playerBounds = _boundarySprite.bounds;
            hasBounds = true;
        }
        if (hasBounds)
        {
            extents = playerBounds.extents;
            offset = (Vector2)playerBounds.center - (Vector2)transform.position;
        }

        float minX = bounds.min.x + extents.x - offset.x;
        float maxX = bounds.max.x - extents.x - offset.x;
        float minY = bounds.min.y + extents.y - offset.y;
        float maxY = bounds.max.y - extents.y - offset.y - Mathf.Max(0f, _topBoundaryPadding);
        Vector2 clamped = ClampPosition(position, minX, maxX, minY, maxY);
        if (clamped != position)
            _playerRigidbody2D.position = clamped;

        if (limitVelocity)
        {
            Vector2 next = clamped + _playerRigidbody2D.linearVelocity * Time.fixedDeltaTime;
            _playerRigidbody2D.linearVelocity =
                (ClampPosition(next, minX, maxX, minY, maxY) - clamped) / Time.fixedDeltaTime;
        }
    }

    private static Vector2 ClampPosition(Vector2 position, float minX, float maxX, float minY, float maxY)
    {
        return new Vector2(
            minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : (minX + maxX) * 0.5f,
            minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : (minY + maxY) * 0.5f);
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

        // 이동 속도에 맞춘 간격으로 발걸음 재생. 멈추면 예약 취소.
        float speed = _playerRigidbody2D.linearVelocity.magnitude;
        if (moveDirection != Vector2.zero && speed > 0.5f)
        {
            _stepTimer -= Time.fixedDeltaTime;
            if (_stepTimer <= 0f)
            {
                Sfx.PlayFootstep();
                _stepTimer = Mathf.Clamp(StrideLength / speed, MinStepInterval, MaxStepInterval);
            }
        }
        else
        {
            _stepTimer = 0f;
        }
    }
}
