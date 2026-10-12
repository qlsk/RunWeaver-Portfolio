using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMotor), typeof(EnemyTargetDetector))]
public class EnemyRockfallAttack : MonoBehaviour
{
    [SerializeField] private bool _externallyControlled;
    [SerializeField, Min(0f)] private float _attackRange = 10f;
    [SerializeField, Min(1)] private int _rockCount = 3;
    [SerializeField, Min(0.01f)] private float _markInterval = 0.45f;
    [SerializeField, Min(0.01f)] private float _warningDuration = 1f;
    [SerializeField, Min(0.01f)] private float _attackRadius = 1.2f;
    [SerializeField, Range(0.1f, 1f)] private float _rangeHeightRatio = 1f;
    [SerializeField, Min(0f)] private float _damage = 15f;
    [SerializeField, Min(0f)] private float _cooldown = 3f;
    [SerializeField] private Sprite _rangeImage;
    [SerializeField] private Sprite _rockSprite;
    [SerializeField] private Color _rangeColor = new Color(1f, 0.3f, 0.1f, 0.4f);
    [SerializeField] private Color _rockColor = new Color(0.45f, 0.3f, 0.2f, 1f);
    [SerializeField, Min(0.01f)] private float _riseDuration = 0.15f;
    [SerializeField, Min(0f)] private float _holdDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float _sinkDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float _rockHeight = 2f;
    [SerializeField] private string _sortingLayer = "Default";
    [SerializeField] private int _sortingOrder = 10;

    private sealed class Strike
    {
        public Vector2 Center;
        public float ImpactTime;
        public SpriteRenderer Warning;
        public SpriteRenderer Rock;
        public RockEruptionVisual Visual;
        public bool HasHit;
    }

    private readonly List<Strike> _pending = new List<Strike>();
    private readonly List<Collider2D> _hits = new List<Collider2D>();
    private readonly HashSet<PlayerHealth> _damagedPlayers = new HashSet<PlayerHealth>();
    private readonly HashSet<DefenseAltar> _damagedAltars = new HashSet<DefenseAltar>();
    private EnemyMotor _motor;
    private EnemyTargetDetector _targetDetector;
    private EnemyHealth _health;
    private EnemyStatusEffectController _statusEffects;
    private int _remainingMarks;
    private float _nextMarkTime, _nextAttackTime, _pendingRadius, _pendingDamage;
    private float _pendingWarning, _pendingInterval;
    private float _pendingHeightRatio;
    private EllipseAttackArea _attackArea;
    public EnemyAttackState State { get; private set; }
    public bool IsAttackActive => isActiveAndEnabled && State != EnemyAttackState.Idle;
    public event System.Action<EnemyAttackEndReason> AttackEnded;
    public event System.Action RockErupted;
    public bool AnimationDriven { get; set; }
    public float WarningDuration => Mathf.Max(0.01f, _warningDuration);
    private bool _released;
    private float _castEndTime;

    public void AnimationImpact()
    {
        if (!AnimationDriven || !IsAttackActive || State != EnemyAttackState.Attacking || _released) return;
        _released = true;
        _nextMarkTime = Time.time;
    }

    private void Awake()
    {
        _motor = GetComponent<EnemyMotor>();
        _targetDetector = GetComponent<EnemyTargetDetector>();
        _health = GetComponent<EnemyHealth>();
        _statusEffects = GetComponentInParent<EnemyStatusEffectController>();
        _attackArea = new EllipseAttackArea(gameObject);
        if (_rangeImage == null || _rangeImage.bounds.size.x <= 0f || _rangeImage.bounds.size.y <= 0f)
        {
            Debug.LogWarning("EnemyRockfallAttack: Range Image에 원형 Sprite를 넣으세요.", this);
            enabled = false;
        }
    }

    public void SetExternalControl(bool value)
    {
        if (_externallyControlled == value) return;
        CancelAttack();
        _externallyControlled = value;
    }

    public bool TryStartAttack()
    {
        if (!isActiveAndEnabled || State != EnemyAttackState.Idle || _rangeImage == null ||
            _targetDetector == null || _targetDetector.Target == null || Time.time < _nextAttackTime ||
            (_health != null && _health.IsDead) || (_statusEffects != null && _statusEffects.IsStaggered))
            return false;
        if (((Vector2)(_targetDetector.Target.position - transform.position)).sqrMagnitude > _attackRange * _attackRange)
            return false;

        _remainingMarks = Mathf.Max(1, _rockCount);
        _pendingRadius = Mathf.Max(0.01f, _attackRadius);
        _pendingHeightRatio = Mathf.Clamp(_rangeHeightRatio, 0.1f, 1f);
        _pendingDamage = Mathf.Max(0f, _damage);
        _pendingWarning = Mathf.Max(0.01f, _warningDuration);
        _pendingInterval = Mathf.Max(0.01f, _markInterval);
        _nextMarkTime = Time.time;
        State = EnemyAttackState.Preparing;
        _released = !AnimationDriven;
        _castEndTime = Time.time + _pendingWarning;
        _motor.AcquirePositionLock(this);
        return true;
    }

    private void FixedUpdate()
    {
        if ((_health != null && _health.IsDead) || _targetDetector.Target == null ||
            (_statusEffects != null && _statusEffects.IsStaggered))
        {
            CancelAttack();
            return;
        }
        if (State == EnemyAttackState.Idle)
        {
            if (!_externallyControlled) TryStartAttack();
            return;
        }

        if (AnimationDriven && !_released)
        {
            if (Time.time >= _castEndTime) State = EnemyAttackState.Attacking;
            if (Time.time >= _castEndTime + 3f) CancelAttack();
            return;
        }
        // 표식마다 현재 위치를 새로 기록하지만 이미 표시한 중심은 바꾸지 않는다.
        if (_remainingMarks > 0 && Time.time >= _nextMarkTime)
        {
            Vector2 center = _targetDetector.Target.position;
            var strike = new Strike { Center = center, ImpactTime = Time.time + _pendingWarning };
            strike.Warning = CreateVisual("RockfallWarning", _rangeImage, center, _rangeColor, _sortingOrder);
            Vector3 warningScale = strike.Warning.transform.localScale;
            warningScale.y *= _pendingHeightRatio;
            strike.Warning.transform.localScale = warningScale;
            PositionVisual(strike.Warning, center);
            if (_rockSprite != null)
            {
                strike.Rock = CreateVisual("EmergingRock", _rockSprite, center, _rockColor, _sortingOrder + 1);
                strike.Visual = new GameObject("RockEruption").AddComponent<RockEruptionVisual>();
                strike.Visual.Initialize(strike.Rock, new Vector3(center.x, center.y, transform.position.z), _rockHeight);
                strike.Visual.gameObject.SetActive(false);
            }
            _pending.Add(strike);
            _remainingMarks--;
            _nextMarkTime = Time.time + _pendingInterval;
        }

        for (int i = 0; i < _pending.Count;)
        {
            Strike strike = _pending[i];
            float remaining = strike.ImpactTime - Time.time;
            if (remaining > 0f)
            {
                i++;
                continue;
            }
            if (!strike.HasHit)
            {
                strike.HasHit = true;
                DestroyVisual(strike.Warning);
                State = EnemyAttackState.Attacking;
                RockErupted?.Invoke();
                if (State == EnemyAttackState.Idle) return;
                ApplyDamage(strike.Center);
                if (State == EnemyAttackState.Idle) return;
            }
            float elapsed = -remaining;
            float rise = Mathf.Max(0.01f, _riseDuration);
            float sink = Mathf.Max(0.01f, _sinkDuration);
            float hold = Mathf.Max(0f, _holdDuration);
            if (elapsed < rise + hold + sink)
            {
                if (strike.Visual != null)
                {
                    strike.Visual.gameObject.SetActive(true);
                    strike.Visual.Animate(elapsed, rise, hold, sink);
                }
                i++;
                continue;
            }
            _pending.RemoveAt(i);
            DestroyRock(strike);
        }
        if (_remainingMarks == 0 && _pending.Count == 0)
            EndAttack(EnemyAttackEndReason.Completed);
    }

    private SpriteRenderer CreateVisual(string name, Sprite sprite, Vector2 center, Color color, int order)
    {
        var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = _sortingLayer;
        renderer.sortingOrder = order;
        Vector3 size = sprite.bounds.size;
        renderer.transform.localScale = new Vector3(2f * _pendingRadius / Mathf.Max(0.01f, size.x), 2f * _pendingRadius / Mathf.Max(0.01f, size.y), 1f);
        PositionVisual(renderer, center);
        return renderer;
    }

    private void PositionVisual(SpriteRenderer renderer, Vector2 center)
    {
        renderer.transform.position = new Vector3(center.x, center.y, transform.position.z)
                                      - renderer.transform.TransformVector(renderer.sprite.bounds.center);
    }

    private void ApplyDamage(Vector2 center)
    {
        _hits.Clear();
        _damagedPlayers.Clear();
        _damagedAltars.Clear();
        if (_pendingHeightRatio >= 1f)
            Physics2D.OverlapCircle(center, _pendingRadius, new ContactFilter2D { useTriggers = true }, _hits);
        else
            _attackArea.Overlap(center, _pendingRadius, _pendingHeightRatio, _hits);
        foreach (Collider2D hit in _hits)
        {
            if (hit == null) continue;
            if (hit.TryGetComponent(out PlayerHitbox hitbox))
            {
                PlayerHealth player = hitbox.Health;
                if (player != null && !player.IsDead && _damagedPlayers.Add(player)) player.TakeDamage(_pendingDamage);
            }
            else
            {
                DefenseAltar altar = hit.GetComponentInParent<DefenseAltar>();
                if (altar != null && altar.isActiveAndEnabled && !altar.IsDestroyed && _damagedAltars.Add(altar))
                    altar.TakeDamage(_pendingDamage);
            }
            if (State == EnemyAttackState.Idle) return;
        }
    }

    public void CancelAttack() => EndAttack(EnemyAttackEndReason.Cancelled);

    private void EndAttack(EnemyAttackEndReason reason)
    {
        bool wasActive = State != EnemyAttackState.Idle;
        State = EnemyAttackState.Idle;
        _remainingMarks = 0;
        foreach (Strike strike in _pending)
        {
            DestroyVisual(strike.Warning);
            DestroyRock(strike);
        }
        _pending.Clear();
        _motor?.ReleasePositionLock(this);
        if (!wasActive) return;
        _nextAttackTime = Time.time + _cooldown;
        AttackEnded?.Invoke(reason);
    }

    private static void DestroyRock(Strike strike)
    {
        if (strike.Visual != null)
        {
            strike.Visual.gameObject.SetActive(false);
            Destroy(strike.Visual.gameObject);
        }
        else DestroyVisual(strike.Rock);
    }
    private static void DestroyVisual(SpriteRenderer renderer)
    {
        if (renderer == null) return;
        renderer.gameObject.SetActive(false);
        Destroy(renderer.gameObject);
    }

    private void OnEnable() => EnemyHealth.OnDeath += HandleDeath;
    private void OnDestroy() => _attackArea?.Dispose();
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _rangeColor;
        if (_pending.Count == 0)
            EllipseAttackArea.DrawGizmo(transform.position, _attackRadius, _rangeHeightRatio);
        else
            foreach (Strike strike in _pending)
                EllipseAttackArea.DrawGizmo(strike.Center, _pendingRadius, _pendingHeightRatio);
    }
    private void HandleDeath(EnemyHealth health) { if (health == _health) CancelAttack(); }
    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleDeath;
        CancelAttack();
    }
}
