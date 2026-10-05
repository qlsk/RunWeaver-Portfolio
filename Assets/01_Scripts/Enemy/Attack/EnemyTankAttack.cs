using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyTargetDetector))]
public class EnemyTankAttack : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _detectionRange = 3f;
    [SerializeField, Min(0.01f)] private float _attackRadius = 2f;
    [SerializeField, Min(0f)] private float _damage = 20f;
    [SerializeField, Min(0f)] private float _attackCooldown = 2f;
    [SerializeField] private Sprite _rangeImage;
    [SerializeField] private Color _rangeColor = new Color(1f, 0.2f, 0.1f, 0.35f);
    [SerializeField] private string _rangeSortingLayer = "Default";
    [SerializeField] private int _rangeSortingOrder = 10;

    private const float WarningDuration = 1f;
    private EnemyTargetDetector _targetDetector;
    private EnemyHealth _health;
    private EnemyStatusEffectController _statusEffects;
    private SpriteRenderer _indicator;
    private readonly HashSet<PlayerHealth> _damagedPlayers = new HashSet<PlayerHealth>();
    private Vector2 _attackCenter;
    private float _pendingRadius;
    private float _attackTime;
    private float _nextAttackTime;
    public EnemyAttackState State { get; private set; } = EnemyAttackState.Idle;
    public bool IsAttackActive => isActiveAndEnabled && State != EnemyAttackState.Idle;

    private Rigidbody2D _body;
    private RigidbodyConstraints2D _savedConstraints;
    private bool _positionFrozen;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _health = GetComponent<EnemyHealth>();
        _statusEffects = GetComponentInParent<EnemyStatusEffectController>();
        if (_rangeImage == null || _rangeImage.bounds.size.x <= 0f || _rangeImage.bounds.size.y <= 0f)
        {
            Debug.LogWarning("EnemyTankAttack: Range Image에 유효한 원형 Sprite를 넣으세요.", this);
            enabled = false;
            return;
        }

        CreateIndicator();
    }

    private void OnEnable()
    {
        EnemyHealth.OnDeath += HandleDeath;
    }

    private void FixedUpdate()
    {
        if ((_health != null && _health.IsDead) ||
            (_statusEffects != null && _statusEffects.IsStaggered))
        {
            CancelAttack();
            return;
        }

        switch (State)
        {
            case EnemyAttackState.Idle:
                TryPrepare();
                break;
            case EnemyAttackState.Preparing:
                if (Time.time >= _attackTime)
                {
                    State = EnemyAttackState.Attacking;
                    _indicator.enabled = false;
                    ExecuteAttack();
                }

                break;
            case EnemyAttackState.Attacking:
                CancelAttack();
                break;
        }
    }

    private void TryPrepare()
    {
        Transform target = _targetDetector.Target;
        if (target == null || Time.time < _nextAttackTime)
            return;

        Vector2 offset = target.position - transform.position;
        if (offset.sqrMagnitude > _detectionRange * _detectionRange)
            return;

        // 예고한 위치와 반경을 고정해서 플레이어가 범위 밖으로 피할 수 있게 한다.
        _attackCenter = transform.position;
        _pendingRadius = _attackRadius;
        _attackTime = Time.time + WarningDuration;
        State = EnemyAttackState.Preparing;
        _body.linearVelocity = Vector2.zero;
        _savedConstraints = _body.constraints;
        _positionFrozen = true;
        _body.constraints |= RigidbodyConstraints2D.FreezePosition;

        Bounds bounds = _indicator.sprite.bounds;
        float diameter = _pendingRadius * 2f;
        Transform indicator = _indicator.transform;
        indicator.rotation = Quaternion.identity;
        indicator.localScale = new Vector3(diameter / bounds.size.x, diameter / bounds.size.y, 1f);
        indicator.position = new Vector3(_attackCenter.x, _attackCenter.y, transform.position.z)
                             - indicator.TransformVector(bounds.center);
        _indicator.color = _rangeColor;
        _indicator.sortingLayerName = _rangeSortingLayer;
        _indicator.sortingOrder = _rangeSortingOrder;
        _indicator.enabled = true;
    }

    private void ExecuteAttack()
    {
        _body.linearVelocity = Vector2.zero;
        _damagedPlayers.Clear();

        // Trigger를 포함한 플레이어 콜라이더를 검사하고 중복 콜라이더 피해를 방지한다.
        var filter = new ContactFilter2D { useTriggers = true };
        var hits = new List<Collider2D>();
        Physics2D.OverlapCircle(_attackCenter, _pendingRadius, filter, hits);
        foreach (Collider2D hit in hits)
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();
            if (player != null && !player.IsDead && _damagedPlayers.Add(player))
            {
                player.TakeDamage(_damage);
            }
        }
    }

    public void CancelAttack()
    {
        if (State != EnemyAttackState.Idle)
        {
            if (_body != null)
                _body.linearVelocity = Vector2.zero;
            _nextAttackTime = Time.time + _attackCooldown;
        }

        RestoreConstraints();
        if (_indicator != null)
            _indicator.enabled = false;
        State = EnemyAttackState.Idle;
    }

    private void RestoreConstraints()
    {
        if (!_positionFrozen || _body == null)
            return;
        _body.constraints = _savedConstraints;
        _positionFrozen = false;
    }

    private void CreateIndicator()
    {
        var indicatorObject = new GameObject("TankAttackRangeIndicator");
        _indicator = indicatorObject.AddComponent<SpriteRenderer>();
        _indicator.sprite = _rangeImage;
        _indicator.enabled = false;
    }

    private void HandleDeath(EnemyHealth health)
    {
        if (health == _health)
            CancelAttack();
    }

    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleDeath;
        CancelAttack();
    }

    private void OnDestroy()
    {
        if (_indicator != null)
            Destroy(_indicator.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = _rangeColor;
        Gizmos.DrawWireSphere(State != EnemyAttackState.Idle ? (Vector3)_attackCenter : transform.position,
            State != EnemyAttackState.Idle ? _pendingRadius : _attackRadius);
    }
}