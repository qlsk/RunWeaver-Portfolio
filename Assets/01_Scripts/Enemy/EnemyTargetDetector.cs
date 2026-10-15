using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// 피격 후에는 잠시 플레이어를 추적하고, 그 외에는 활성 제단을 우선 선택한다.
public class EnemyTargetDetector : MonoBehaviour
{
    [FoldoutGroup("Settings"), SerializeField] private float _baseTrackingInterval = 0.1f;
    [FoldoutGroup("Settings"), SerializeField, Min(0f)] private float _playerAggroDuration = 5f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private float _trackingTimer;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private float _trackingIntervalMultiplier = 1f;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")]
    private readonly Dictionary<int, float> _trackingIntervalModifiers = new();
    private Transform _fallbackTarget;
    private float _nextPlayerSearchTime;
    private EnemyHealth _health;
    private Transform _aggroTarget;
    private float _playerAggroUntil;

    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] public Transform Target { get; private set; }
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")]
    public Vector2 LastKnownTargetPosition { get; private set; }
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")]
    public float TrackingInterval => _baseTrackingInterval * _trackingIntervalMultiplier;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
        RefreshTarget();
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDamaged += HandleDamaged;
        RefreshTarget();
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDamaged -= HandleDamaged;
        _aggroTarget = null;
        _playerAggroUntil = 0f;
        _trackingTimer = 0f;
    }

    private void HandleDamaged()
    {
        if (_health == null || _health.IsDead || _playerAggroDuration <= 0f) return;

        // 현재 탄환/원소 피해는 이 공통 피격 이벤트를 거치며, 공격자 정보는 전달하지 않는다.
        Transform player = _fallbackTarget;
        if (!IsValidTarget(player) || !player.CompareTag("Player"))
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }

        if (!IsValidTarget(player)) return;
        _fallbackTarget = player;
        _aggroTarget = player;
        _playerAggroUntil = Time.time + _playerAggroDuration;
        RefreshTarget();
    }

    private void FixedUpdate()
    {
        RefreshTarget();
        if (Target == null)
            return;

        _trackingTimer += Time.fixedDeltaTime;
        if (_trackingTimer < TrackingInterval)
            return;

        LastKnownTargetPosition = Target.position;
        _trackingTimer = 0f;
    }

    public void SetTarget(Transform target)
    {
        _fallbackTarget = target;
        RefreshTarget();
    }

    private void RefreshTarget()
    {
        Transform nextTarget;
        if (Time.time < _playerAggroUntil && IsValidTarget(_aggroTarget))
        {
            nextTarget = _aggroTarget;
        }
        else
        {
            _aggroTarget = null;
            _playerAggroUntil = 0f;
            if (!DefenseAltar.TryGetEnemyTarget(transform, out nextTarget))
            {
                if (!IsValidTarget(_fallbackTarget) && Time.time >= _nextPlayerSearchTime)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    _fallbackTarget = player != null ? player.transform : null;
                    _nextPlayerSearchTime = Time.time + 0.5f;
                }

                nextTarget = IsValidTarget(_fallbackTarget) ? _fallbackTarget : null;
            }
        }

        if (Target == nextTarget) return;
        Target = nextTarget;
        _trackingTimer = 0f;
        LastKnownTargetPosition = nextTarget != null ? (Vector2)nextTarget.position : (Vector2)transform.position;
    }

    private static bool IsValidTarget(Transform target)
    {
        return target != null && target.gameObject.activeInHierarchy &&
            (!target.TryGetComponent(out PlayerHealth health) || !health.IsDead);
    }

    public void AddTrackingIntervalModifier(int sourceId, float multiplier)
    {
        _trackingIntervalModifiers[sourceId] = multiplier;
        RefreshTrackingIntervalMultiplier();
    }

    public void RemoveTrackingIntervalModifier(int sourceId)
    {
        _trackingIntervalModifiers.Remove(sourceId);
        RefreshTrackingIntervalMultiplier();
    }

    private void RefreshTrackingIntervalMultiplier()
    {
        _trackingIntervalMultiplier = 1f;
        foreach (float multiplier in _trackingIntervalModifiers.Values)
        {
            _trackingIntervalMultiplier = Mathf.Max(_trackingIntervalMultiplier, multiplier);
        }
    }
}
