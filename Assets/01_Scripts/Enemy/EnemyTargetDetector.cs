using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// 일정 간격으로 대상 위치를 관측하고 출처별 보정으로 관측 간격을 조절한다.
public class EnemyTargetDetector : MonoBehaviour
{
    [FoldoutGroup("Settings"), SerializeField] private float _baseTrackingInterval = 0.1f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private float _trackingTimer;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] private float _trackingIntervalMultiplier = 1f;
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")]
    private readonly Dictionary<int, float> _trackingIntervalModifiers = new();

    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")] public Transform Target { get; private set; }
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")]
    public Vector2 LastKnownTargetPosition { get; private set; }
    [ShowInInspector, ReadOnly, FoldoutGroup("Runtime State")]
    public float TrackingInterval => _baseTrackingInterval * _trackingIntervalMultiplier;

    private void Awake()
    {
        if (Target != null)
            return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            SetTarget(player.transform);
    }

    private void FixedUpdate()
    {
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
        Target = target;
        _trackingTimer = 0f;
        LastKnownTargetPosition = target.position;
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