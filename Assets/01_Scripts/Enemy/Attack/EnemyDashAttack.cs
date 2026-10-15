using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(EnemyMotor), typeof(EnemyTargetDetector))]
public class EnemyDashAttack : MonoBehaviour
{
    [SerializeField] private bool _externallyControlled;
    public event System.Action<EnemyAttackEndReason> AttackEnded;

    public void SetExternalControl(bool value)
    {
        if (_externallyControlled == value) return;
        CancelDash();
        _externallyControlled = value;
    }
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
    [Tooltip("표시기 시작 위치. 비워두면 몸체 Collider의 아래쪽 중앙을 사용합니다.")]
    [SerializeField] private Transform _rangeOrigin;
    [SerializeField, Min(0.01f)] private float _rangeWidth = 0.8f;
    [SerializeField] private Color _rangeColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField, Range(0f, 1f)] private float _chargedRangeAlpha = 0.9f;
    [SerializeField] private string _rangeSortingLayer = "Default";
    [SerializeField] private int _rangeSortingOrder;

    public EnemyAttackState State { get; private set; } = EnemyAttackState.Idle;
    public bool IsDashActive => isActiveAndEnabled && State != EnemyAttackState.Idle;

    private Rigidbody2D _body;
    private Collider2D _bodyCollider;
    private EnemyHealth _health;
    private EnemyStatusEffectController _statusEffectController;
    private Vector2 _targetPosition;
    private EnemyMotor _motor;
    private float _stateEndTime;
    private float _chargeStartTime;
    private float _nextAttackTime;
    private SpriteRenderer _rangeIndicator;
    private AttackRangeChargeVisual _chargeVisual;

    private void Awake()
    {
        _motor = GetComponent<EnemyMotor>();
        _body = GetComponent<Rigidbody2D>();
        _bodyCollider = GetComponent<Collider2D>();
        _health = GetComponent<EnemyHealth>();
        _statusEffectController = GetComponent<EnemyStatusEffectController>();
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
        _chargeVisual = new AttackRangeChargeVisual(_rangeIndicator, false);
        _rangeIndicator.color = _rangeColor;
        _rangeIndicator.sortingLayerName = _rangeSortingLayer;
        _rangeIndicator.sortingOrder = _rangeSortingOrder;
        _rangeIndicator.enabled = false;
    }

    private void OnEnable()
    {
        EnemyHealth.OnDeath += HandleDeath;
    }

    /// 사망 또는 속박 상태에서는 돌진을 취소하고 그 외에는 기존 돌진 상태를 진행한다.
    private void FixedUpdate()
    {
        if ((_health != null && _health.IsDead) ||
            (_statusEffectController != null && _statusEffectController.IsRooted))
        {
            CancelDash();
            return;
        }

        switch (State)
        {
            case EnemyAttackState.Idle:
                if (!_externallyControlled) TryStartAttack();
                break;
            case EnemyAttackState.Preparing:
                UpdateRangeCharge();
                if (Time.time >= _stateEndTime)
                {
                    _motor?.ReleasePositionLock(this);
                    _rangeIndicator.enabled = false;
                    State = EnemyAttackState.Attacking;
                    _stateEndTime = Time.time + _maxDashDuration;
                    _bossStepTimer = 0f;
                    MoveDash();
                }

                break;
            case EnemyAttackState.Attacking:
                MoveDash();
                TickBossSteps();
                break;
        }
    }

    public bool TryStartAttack()
    {
        if (!isActiveAndEnabled || State != EnemyAttackState.Idle || _rangeIndicator == null ||
            (_health != null && _health.IsDead) ||
            (_statusEffectController != null && _statusEffectController.IsRooted))
            return false;
        Transform target = _targetDetector.Target;
        if (target == null || Time.time < _nextAttackTime)
            return false;

        Vector2 start = _body.position;
        Vector2 toTarget = (Vector2)target.position - start;
        float distance = toTarget.magnitude;
        if (distance > _attackRange || distance <= _arrivalDistance)
            return false;

        _targetPosition = start + toTarget.normalized * _dashDistance;
        State = EnemyAttackState.Preparing;
        _motor.AcquirePositionLock(this);
        _chargeStartTime = Time.time;
        _stateEndTime = _chargeStartTime + _prepareDuration;

        ShowRangeIndicator(start);
        return true;
    }

    private void ShowRangeIndicator(Vector2 start)
    {
        UpdateRangeCharge();
        _rangeIndicator.sortingLayerName = _rangeSortingLayer;
        _rangeIndicator.sortingOrder = _rangeSortingOrder;
        Bounds bounds = _rangeIndicator.sprite.bounds;
        if (bounds.size.x <= 0f || bounds.size.y <= 0f)
            return;

        Vector2 path = _targetPosition - start;
        Vector2 rangeStart = start;
        if (_rangeOrigin != null)
            rangeStart = _rangeOrigin.position;
        else if (_bodyCollider != null && _bodyCollider.enabled)
        {
            Bounds bodyBounds = _bodyCollider.bounds;
            rangeStart = new Vector2(bodyBounds.center.x, bodyBounds.min.y);
        }

        Vector2 center = rangeStart + path * 0.5f;
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
            GetRangeWidth(path) / (bounds.size.y * yScale), 1f);

        Vector3 localCenter = bounds.center;
        if (_rangeIndicator.flipX) localCenter.x = -localCenter.x;
        if (_rangeIndicator.flipY) localCenter.y = -localCenter.y;
        indicator.position = new Vector3(center.x, center.y, transform.position.z)
                             - indicator.TransformVector(localCenter);
        _rangeIndicator.enabled = true;
    }

    private float GetRangeWidth(Vector2 path)
    {
        if (_bodyCollider == null || !_bodyCollider.enabled ||
            !_bodyCollider.gameObject.activeInHierarchy)
            return _rangeWidth;

        // A circle has the same world-space diameter in every dash direction.
        if (_bodyCollider is CircleCollider2D)
            return _bodyCollider.bounds.size.x * 2;

        Vector2 perpendicular = new Vector2(-path.y, path.x).normalized;
        if (_bodyCollider is BoxCollider2D box)
        {
            Vector2 horizontal = box.transform.TransformVector(Vector3.right * box.size.x);
            Vector2 vertical = box.transform.TransformVector(Vector3.up * box.size.y);
            return 2 * Mathf.Abs(Vector2.Dot(perpendicular, horizontal)) +
                   2 * Mathf.Abs(Vector2.Dot(perpendicular, vertical));
        }

        // Use the projected world bounds for other body collider shapes.
        Vector2 size = _bodyCollider.bounds.size;
        return 2 * Mathf.Abs(perpendicular.x) * size.x + 2 * Mathf.Abs(perpendicular.y) * size.y;
    }
    private void UpdateRangeCharge()
    {
        float duration = _stateEndTime - _chargeStartTime;
        float progress = duration > 0f ? Mathf.Clamp01((Time.time - _chargeStartTime) / duration) : 1f;
        _chargeVisual.SetProgress(progress, _rangeColor, _chargedRangeAlpha);
    }

    private void MoveDash()
    {
        Vector2 offset = _targetPosition - _body.position;
        float distance = offset.magnitude;
        if (distance <= _arrivalDistance || Time.time >= _stateEndTime)
        {
            EndAttack(EnemyAttackEndReason.Completed);
            return;
        }

        float speed = Mathf.Min(_dashSpeed, distance / Time.fixedDeltaTime);
        if (!_motor.SetOverrideVelocity(this, offset.normalized * speed))
            CancelDash();
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
        if (State == EnemyAttackState.Attacking &&
            (_obstacleLayers.value & (1 << layer)) != 0)
        {
            EndAttack(EnemyAttackEndReason.Obstacle);
        }
    }

    // 사망, 비활성화 또는 외부 상태이상에서 돌진을 취소할 때 사용한다.
    public void CancelDash()
    {
        EndAttack(EnemyAttackEndReason.Cancelled);
    }

    private void EndAttack(EnemyAttackEndReason reason)
    {
        bool wasActive = State != EnemyAttackState.Idle;
        if (wasActive)
        {
            _motor?.ClearOverride(this);
            _nextAttackTime = Time.time + _cooldown;
        }

        _motor?.ReleasePositionLock(this);
        if (_rangeIndicator != null)
        {
            _rangeIndicator.enabled = false;
        }

        State = EnemyAttackState.Idle;
        if (wasActive) AttackEnded?.Invoke(reason);
    }

    private void HandleDeath(EnemyHealth health)
    {
        if (health == _health)
        {
            CancelDash();
        }
    }

    /// <summary>보스만 이동 루프음을 낸다. 일반몹 Dash와 공유하므로 컨트롤러 동거로 판정.</summary>
    private bool IsBossDash()
    {
        return GetComponent<BossPatternController>() != null;
    }

    private float _bossStepTimer;
    private const float BossStepInterval = 0.22f;

    /// <summary>보스 돌진 중 발걸음을 빠르게 반복한다.</summary>
    private void TickBossSteps()
    {
        if (!IsBossDash()) return;
        _bossStepTimer -= Time.fixedDeltaTime;
        if (_bossStepTimer <= 0f)
        {
            _bossStepTimer = BossStepInterval;
            Sfx.Play(Sfx.BossFootstep);
        }
    }

    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleDeath;
        CancelDash();
    }

    private void OnDestroy()
    {
        _chargeVisual?.Dispose();
        if (_rangeIndicator != null)
            Destroy(_rangeIndicator.gameObject);
    }
}
