using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// 활성 제단을 우선 추적하고 일정 간격으로 대상 위치를 관측한다.
public class EnemyTargetDetector : MonoBehaviour
{
    [FoldoutGroup("Settings"), SerializeField] private float _baseTrackingInterval = 0.1f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private float _trackingTimer;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private float _trackingIntervalMultiplier = 1f;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")]
    private readonly Dictionary<int, float> _trackingIntervalModifiers = new();
    private Transform _fallbackTarget;
    private float _nextPlayerSearchTime;

    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] public Transform Target { get; private set; }
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")]
    public Vector2 LastKnownTargetPosition { get; private set; }
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")]
    public float TrackingInterval => _baseTrackingInterval * _trackingIntervalMultiplier;

    private void Awake()
    {
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
        if (!DefenseAltar.TryGetEnemyTarget(transform, out nextTarget))
        {
            if ((_fallbackTarget == null || !_fallbackTarget.gameObject.activeInHierarchy) &&
                Time.time >= _nextPlayerSearchTime)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                _fallbackTarget = player != null ? player.transform : null;
                _nextPlayerSearchTime = Time.time + 0.5f;
            }

            nextTarget = _fallbackTarget != null && _fallbackTarget.gameObject.activeInHierarchy
                ? _fallbackTarget : null;
        }

        if (Target == nextTarget) return;
        Target = nextTarget;
        _trackingTimer = 0f;
        LastKnownTargetPosition = nextTarget != null ? (Vector2)nextTarget.position : (Vector2)transform.position;
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
