using UnityEngine;

/// <summary>
/// 플레이어 몸통 애니메이션과 좌우 반전을 맡는다.
/// - 달리기 여부와 속도는 입력이 아니라 실제 이동 속도(Rigidbody2D)로 정한다. 벽에 막히거나 경직이면 Idle.
/// - 달리기 재생 속도는 이동 속도에 비례한다. (최고 속도에서 _runAnimSpeedAtMaxSpeed 배)
/// - 마우스 반대쪽으로 움직이면(뒷걸음질) 달리기를 역재생한다. 위아래로만 움직이면 정방향.
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    private static readonly int _isRunning = Animator.StringToHash("isRunning");
    private static readonly int _runSpeed = Animator.StringToHash("runSpeed");

    [Tooltip("이 속도(유닛/초)로 움직일 때 달리기를 _runAnimSpeedAtMaxSpeed 배로 재생한다. (기본 이동 속도)")]
    [SerializeField, Min(0.1f)] private float _referenceMoveSpeed = 5f;
    [Tooltip("기본 이동 속도에서의 달리기 재생 배속. 그림 보폭에 완전히 맞추면 약 3.3배지만 발이 너무 빨라진다.")]
    [SerializeField, Min(0.1f)] private float _runAnimSpeedAtMaxSpeed = 1.35f;
    [SerializeField, Min(0f)] private float _minRunAnimSpeed = 0.5f;
    [SerializeField, Min(0.1f)] private float _maxRunAnimSpeed = 2f;
    [Tooltip("이 속도(유닛/초) 이하이면 멈춘 것으로 본다")]
    [SerializeField, Min(0f)] private float _movingThreshold = 0.1f;
    [Tooltip("가로 이동이 전체 이동의 이 비율 이상이고 마우스 반대쪽이면 뒷걸음질로 본다")]
    [SerializeField, Range(0f, 1f)] private float _backpedalHorizontalRatio = 0.3f;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private PlayerInputHandler _playerInputHandler;
    private Rigidbody2D _rigidbody;
    private PlayerArm _arm;
    private Transform _circle;
    private float _circleOriginX;
    private bool _hasRunSpeedParameter;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _playerInputHandler = GetComponent<PlayerInputHandler>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _arm = GetComponent<PlayerArm>();
        _circle = transform.Find("Circle");
        if (_circle != null)
            _circleOriginX = _circle.localPosition.x;

        // 마을 플레이어처럼 runSpeed 파라미터가 없는 컨트롤러도 있다.
        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.nameHash == _runSpeed && parameter.type == AnimatorControllerParameterType.Float)
                _hasRunSpeedParameter = true;
        }
    }

    private void Update()
    {
        Vector2 mousePosition = _playerInputHandler.MousePosition;

        // 기본 스프라이트가 왼쪽을 바라보고 있으므로, 마우스가 플레이어 기준 오른쪽에 위치하면 스프라이트를 뒤집는다.
        bool isMouseOnPlayerRight = mousePosition.x - transform.position.x > 0;
        _spriteRenderer.flipX = isMouseOnPlayerRight;
        if (_circle != null)
        {
            Vector3 circlePosition = _circle.localPosition;
            circlePosition.x = isMouseOnPlayerRight ? -_circleOriginX : _circleOriginX;
            _circle.localPosition = circlePosition;
        }

        UpdateRun(isMouseOnPlayerRight);
    }

    private void UpdateRun(bool isFacingRight)
    {
        Vector2 velocity = _rigidbody != null
            ? _rigidbody.linearVelocity
            : _playerInputHandler.MoveDirection * _referenceMoveSpeed;
        float speed = velocity.magnitude;
        bool isMoving = speed > _movingThreshold && _playerInputHandler.MoveDirection != Vector2.zero;
        _animator.SetBool(_isRunning, isMoving);

        if (!_hasRunSpeedParameter)
            return;

        if (!isMoving)
        {
            _animator.SetFloat(_runSpeed, 1f);
            return;
        }

        float animSpeed = Mathf.Clamp(speed / _referenceMoveSpeed * _runAnimSpeedAtMaxSpeed,
            _minRunAnimSpeed, _maxRunAnimSpeed);

        float facing = isFacingRight ? 1f : -1f;
        bool isBackpedaling = velocity.x * facing < 0f &&
                              Mathf.Abs(velocity.x) >= speed * _backpedalHorizontalRatio;
        _animator.SetFloat(_runSpeed, isBackpedaling ? -animSpeed : animSpeed);
    }
}
