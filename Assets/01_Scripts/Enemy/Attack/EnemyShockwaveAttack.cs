using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(EnemyMotor))]
public class EnemyShockwaveAttack : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float _warningDuration = 1f;
    [SerializeField, Min(0.01f)] private float _maxRadius = 12f;
    [SerializeField, Min(0.01f)] private float _speed = 7f;
    [SerializeField, Min(0.01f)] private float _ringWidth = 0.45f;
    [SerializeField, Min(0f)] private float _damage = 20f;
    [SerializeField, Min(0f)] private float _cooldown = 8f;
    [SerializeField] private Color _color = new Color(0.3f, 1f, 0.75f, 0.9f);
    private EnemyMotor _motor;
    private EnemyHealth _health;
    private EnemyStatusEffectController _status;
    private ShockwaveRingVisual _ring;
    private Vector2 _center;
    private float _radius, _readyTime, _nextAttackTime;
    private readonly List<Collider2D> _hits = new List<Collider2D>();
    private readonly HashSet<PlayerHealth> _crossed = new HashSet<PlayerHealth>();
    private readonly Dictionary<PlayerHealth, float> _previousDistances = new Dictionary<PlayerHealth, float>();
    public EnemyAttackState State { get; private set; }
    public bool IsAttackActive => isActiveAndEnabled && State != EnemyAttackState.Idle;
    public float WarningDuration => Mathf.Max(0.01f, _warningDuration);
    public event System.Action<EnemyAttackEndReason> AttackEnded;

    private void Awake()
    {
        _motor = GetComponent<EnemyMotor>();
        _health = GetComponent<EnemyHealth>();
        _status = GetComponentInParent<EnemyStatusEffectController>();
        _ring = new GameObject("BossShockwaveRing").AddComponent<ShockwaveRingVisual>();
        _ring.gameObject.SetActive(false);
    }

    public bool TryStartAttack()
    {
        if (!isActiveAndEnabled || State != EnemyAttackState.Idle || _ring == null || Time.time < _nextAttackTime ||
            (_health != null && _health.IsDead) || (_status != null && _status.IsStaggered)) return false;
        _center = transform.position;
        _radius = 0f;
        _crossed.Clear();
        _previousDistances.Clear();
        _readyTime = Time.time + WarningDuration;
        State = EnemyAttackState.Preparing;
        _motor.AcquirePositionLock(this);
        DrawRing(0.7f, new Color(_color.r, _color.g, _color.b, 0.35f));
        return true;
    }

    private void FixedUpdate()
    {
        if (!IsAttackActive) return;
        if ((_health != null && _health.IsDead) || (_status != null && _status.IsStaggered)) { CancelAttack(); return; }
        if (State == EnemyAttackState.Preparing)
        {
            if (Time.time < _readyTime) return;
            State = EnemyAttackState.Attacking;
        }
        float previous = _radius;
        _radius = Mathf.Min(_maxRadius, _radius + Mathf.Max(0.01f, _speed) * Time.fixedDeltaTime);
        DrawRing(_radius, _color);
        CheckRing(previous);
        if (State != EnemyAttackState.Idle && _radius >= _maxRadius) EndAttack(EnemyAttackEndReason.Completed);
    }

    private void CheckRing(float previousRadius)
    {
        _hits.Clear();
        float halfWidth = Mathf.Max(0.01f, _ringWidth) * 0.5f;
        Physics2D.OverlapCircle(_center, _maxRadius + halfWidth, new ContactFilter2D { useTriggers = true }, _hits);
        foreach (Collider2D hit in _hits)
        {
            if (hit == null || !hit.TryGetComponent(out PlayerHitbox hitbox)) continue;
            PlayerHealth player = hitbox.Health;
            if (player == null || player.IsDead || _crossed.Contains(player)) continue;
            float distance = Vector2.Distance(_center, hit.bounds.center);
            float extent = ((Vector2)hit.bounds.extents).magnitude;
            float nearest = Vector2.Distance(_center, hit.ClosestPoint(_center));
            bool touching = nearest <= _radius + halfWidth && distance + extent >= previousRadius - halfWidth;
            bool crossed = _previousDistances.TryGetValue(player, out float oldDistance) &&
                           oldDistance > previousRadius + halfWidth && distance < _radius - halfWidth;
            _previousDistances[player] = distance;
            if (!touching && !crossed) continue;
            // 대시 무적이면 TakeDamage가 무시되며, 통과한 파동은 다시 때리지 않는다.
            _crossed.Add(player);
            player.TakeDamage(_damage);
            if (State == EnemyAttackState.Idle) return;
        }
    }

    private void DrawRing(float radius, Color color)
    {
        _ring.gameObject.SetActive(true);
        _ring.transform.position = new Vector3(_center.x, _center.y, transform.position.z);
        float fade = State == EnemyAttackState.Preparing ? 1f : Mathf.Clamp01((_maxRadius - radius) / 1.2f);
        _ring.Draw(radius, Mathf.Max(0.01f, _ringWidth), color, fade);
    }

    public void CancelAttack() => EndAttack(EnemyAttackEndReason.Cancelled);
    private void EndAttack(EnemyAttackEndReason reason)
    {
        bool active = State != EnemyAttackState.Idle;
        State = EnemyAttackState.Idle;
        if (_ring != null) _ring.gameObject.SetActive(false);
        _motor?.ReleasePositionLock(this);
        _crossed.Clear();
        _previousDistances.Clear();
        if (!active) return;
        _nextAttackTime = Time.time + _cooldown;
        AttackEnded?.Invoke(reason);
    }
    private void OnEnable() => EnemyHealth.OnDeath += OnDeath;
    private void OnDeath(EnemyHealth health) { if (health == _health) CancelAttack(); }
    private void OnDisable() { EnemyHealth.OnDeath -= OnDeath; CancelAttack(); }
    private void OnDestroy()
    {
        if (_ring != null) Destroy(_ring.gameObject);
    }
}
