using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyTargetDetector))]
public class EnemyDashAttack : MonoBehaviour
{
    [SerializeField] private EnemyTargetDetector _targetDetector;
    [SerializeField, Min(0.01f)] private float _attackRange = 5f;
    [SerializeField, Min(0.01f)] private float _dashDistance = 5f;
    [SerializeField, Min(0f)] private float _prepareDuration = 1f;
    [SerializeField, Min(0.01f)] private float _dashSpeed = 12f;
    [SerializeField, Min(0f)] private float _cooldown = 2f;
    [SerializeField, Min(0.01f)] private float _maxDashDuration = 3f;
    [SerializeField, Min(0.001f)] private float _arrivalDistance = 0.05f;
    [SerializeField] private LayerMask _obstacleLayers;

    [SerializeField] private Sprite _rangeImage;
    [SerializeField, Min(0.01f)] private float _rangeWidth = 0.8f;
    [SerializeField] private Color _rangeColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private string _rangeSortingLayer = "Default";
    [SerializeField] private int _rangeSortingOrder;

    public DashAttackState State { get; private set; } = DashAttackState.Idle;
    public bool IsDashActive => isActiveAndEnabled && State != DashAttackState.Idle;

    private Rigidbody2D _body;
    private EnemyHealth _health;
    private Vector2 _targetPosition;
    private RigidbodyConstraints2D _savedConstraints;
    private bool _positionFrozen;
    private float _stateEndTime;
    private float _nextAttackTime;
    private SpriteRenderer _rangeIndicator;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _health = GetComponent<EnemyHealth>();
        if (_targetDetector == null)
            _targetDetector = GetComponent<EnemyTargetDetector>();

        if (_rangeImage == null)
        {
            Debug.LogWarning("EnemyDashAttack: Range Image에 이미지(Sprite)를 넣으세요.", this);
            enabled = false;
            return;
        }

        var indicator = new GameObject("DashRangeIndicator");
        indicator.transform.SetParent(transform, false);
        _rangeIndicator = indicator.AddComponent<SpriteRenderer>();
        _rangeIndicator.sprite = _rangeImage;
        _rangeIndicator.color = _rangeColor;
        _rangeIndicator.sortingLayerName = _rangeSortingLayer;
        _rangeIndicator.sortingOrder = _rangeSortingOrder;
        _rangeIndicator.enabled = false;
    }

    private void OnEnable()
    {
        EnemyHealth.OnDeath += HandleDeath;
    }

    private void FixedUpdate()
    {
        if (_health != null && _health.IsDead)
        {
            CancelDash();
            return;
        }

        switch (State)
        {
            case DashAttackState.Idle:
                TryPrepare();
                break;
            case DashAttackState.Preparing:
                if (Time.time >= _stateEndTime)
                {
                    RestoreConstraints();
                    _rangeIndicator.enabled = false;
                    State = DashAttackState.Dashing;
                    _stateEndTime = Time.time + _maxDashDuration;
                    MoveDash();
                }

                break;
            case DashAttackState.Dashing:
                MoveDash();
                break;
        }
    }

    private void TryPrepare()
    {
        Transform target = _targetDetector.Target;
        if (target == null || Time.time < _nextAttackTime)
            return;

        Vector2 start = _body.position;
        Vector2 toTarget = (Vector2)target.position - start;
        float distance = toTarget.magnitude;
        if (distance > _attackRange || distance <= _arrivalDistance)
            return;

        _targetPosition = start + toTarget.normalized * _dashDistance;
        State = DashAttackState.Preparing;
        _body.linearVelocity = Vector2.zero;
        _savedConstraints = _body.constraints;
        _positionFrozen = true;
        _body.constraints |= RigidbodyConstraints2D.FreezePosition;
        _stateEndTime = Time.time + _prepareDuration;

        ShowRangeIndicator(start);
    }

    private void ShowRangeIndicator(Vector2 start)
    {
        _rangeIndicator.color = _rangeColor;
        _rangeIndicator.sortingLayerName = _rangeSortingLayer;
        _rangeIndicator.sortingOrder = _rangeSortingOrder;
        Bounds bounds = _rangeIndicator.sprite.bounds;
        if (bounds.size.x <= 0f || bounds.size.y <= 0f)
            return;

        Vector2 path = _targetPosition - start;
        Vector2 center = (start + _targetPosition) * 0.5f;
        Transform indicator = _rangeIndicator.transform;
        indicator.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(path.y, path.x) * Mathf.Rad2Deg);
        indicator.localScale = Vector3.one;

        float xScale = indicator.TransformVector(Vector3.right).magnitude;
        float yScale = indicator.TransformVector(Vector3.up).magnitude;
        if (xScale <= Mathf.Epsilon || yScale <= Mathf.Epsilon)
            return;

        indicator.localScale = new Vector3(
            path.magnitude / (bounds.size.x * xScale),
            _rangeWidth / (bounds.size.y * yScale), 1f);

        Vector3 localCenter = bounds.center;
        if (_rangeIndicator.flipX) localCenter.x = -localCenter.x;
        if (_rangeIndicator.flipY) localCenter.y = -localCenter.y;
        indicator.position = new Vector3(center.x, center.y, transform.position.z)
                             - indicator.TransformVector(localCenter);
        _rangeIndicator.enabled = true;
    }

    private void MoveDash()
    {
        Vector2 offset = _targetPosition - _body.position;
        float distance = offset.magnitude;
        if (distance <= _arrivalDistance || Time.time >= _stateEndTime)
        {
            CancelDash();
            return;
        }

        float speed = Mathf.Min(_dashSpeed, distance / Time.fixedDeltaTime);
        _body.linearVelocity = offset.normalized * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        StopAtObstacle(collision.gameObject.layer);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        StopAtObstacle(collision.gameObject.layer);
    }

    private void StopAtObstacle(int layer)
    {
        if (State == DashAttackState.Dashing &&
            (_obstacleLayers.value & (1 << layer)) != 0)
        {
            CancelDash();
        }
    }

    // 사망, 비활성화 또는 외부 상태이상에서 돌진을 취소할 때 사용한다.
    public void CancelDash()
    {
        if (State != DashAttackState.Idle)
        {
            _body.linearVelocity = Vector2.zero;
            _nextAttackTime = Time.time + _cooldown;
        }

        RestoreConstraints();
        if (_rangeIndicator != null)
        {
            _rangeIndicator.enabled = false;
        }

        State = DashAttackState.Idle;
    }

    private void RestoreConstraints()
    {
        if (!_positionFrozen || _body == null)
            return;

        _body.constraints = _savedConstraints;
        _positionFrozen = false;
    }

    private void HandleDeath(EnemyHealth health)
    {
        if (health == _health)
        {
            CancelDash();
        }
    }

    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleDeath;
        CancelDash();
    }

    private void OnDestroy()
    {
        if (_rangeIndicator != null)
            Destroy(_rangeIndicator.gameObject);
    }
}