using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 증기 범위에 주기적으로 피해를 적용하고 범위 내 적의 대상 위치 관측 간격을 늘린다.
/// </summary>
public class SteamArea : MonoBehaviour
{
    [FoldoutGroup("Settings"), SerializeField] private float _rangeCheckInterval = 0.1f;

    [FoldoutGroup("References"), SerializeField] private ReactionAreaVisuals _areaVisuals;

    private float _tickDamage;
    private float _radius;
    private float _tickInterval;
    private float _trackingIntervalMultiplier;

    private int _sourceId;
    private float _remainingDuration;
    private float _tickTimer;
    private float _rangeCheckTimer;
    private bool _isInitialized;
    private readonly HashSet<EnemyTargetDetector> _affectedDetectors = new();

    public void Initialize(float tickDamage, float radius, float tickInterval, float duration,
        float trackingIntervalMultiplier)
    {
        ClearTrackingModifiers();

        _tickDamage = tickDamage;
        _radius = radius;
        _tickInterval = tickInterval;
        _remainingDuration = duration;
        _trackingIntervalMultiplier = trackingIntervalMultiplier;
        _sourceId = GetInstanceID();
        _tickTimer = 0f;
        _rangeCheckTimer = 0f;
        _isInitialized = true;
        if (_areaVisuals != null)
            _areaVisuals.Initialize(_radius);

        if (isActiveAndEnabled)
            RefreshTrackingModifiers();
    }

    private void OnEnable()
    {
        if (!_isInitialized)
            return;

        _rangeCheckTimer = 0f;
        RefreshTrackingModifiers();
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        float elapsed = Mathf.Min(Time.deltaTime, _remainingDuration);
        _remainingDuration = Mathf.Max(0f, _remainingDuration - elapsed);
        _rangeCheckTimer += elapsed;
        _tickTimer += elapsed;

        if (_rangeCheckTimer >= _rangeCheckInterval)
        {
            _rangeCheckTimer %= _rangeCheckInterval;
            RefreshTrackingModifiers();
        }

        while (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyTickDamage();
        }

        if (_remainingDuration <= 0f)
        {
            _isInitialized = false;
            ClearTrackingModifiers();
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        ClearTrackingModifiers();
    }

    private void OnDestroy()
    {
        ClearTrackingModifiers();
    }

    private void RefreshTrackingModifiers()
    {
        HashSet<EnemyTargetDetector> currentDetectors = new();

        foreach (EnemyHealth enemy in FindDamageableEnemies())
        {
            if (!enemy.TryGetComponent(out EnemyTargetDetector detector))
                continue;
            currentDetectors.Add(detector);

            if (!_affectedDetectors.Contains(detector))
                detector.AddTrackingIntervalModifier(_sourceId, _trackingIntervalMultiplier);
        }

        IEnumerable<EnemyTargetDetector> exitedDetectors = _affectedDetectors.Where(detector =>
            detector != null && !currentDetectors.Contains(detector));
        foreach (EnemyTargetDetector detector in exitedDetectors)
        {
            detector.RemoveTrackingIntervalModifier(_sourceId);
        }

        _affectedDetectors.Clear();
        _affectedDetectors.UnionWith(currentDetectors);
    }

    private void ApplyTickDamage()
    {
        IEnumerable<EnemyHealth> damageableEnemies = FindDamageableEnemies()
            .Where(enemy => enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled);
        foreach (EnemyHealth enemy in damageableEnemies)
        {
            enemy.TakeDamage(_tickDamage);
        }
    }

    private void ClearTrackingModifiers()
    {
        foreach (EnemyTargetDetector detector in _affectedDetectors.Where(detector => detector != null))
        {
            detector.RemoveTrackingIntervalModifier(_sourceId);
        }

        _affectedDetectors.Clear();
    }

    private HashSet<EnemyHealth> FindDamageableEnemies()
    {
        Collider2D[] colliders = ReactionAreaShape.OverlapAll(transform.position, _radius);
        HashSet<EnemyHealth> enemies = new();

        foreach (Collider2D collider in colliders)
        {
            if (!collider.TryGetComponent(out EnemyHealth enemy))
                continue;
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled)
                continue;
            enemies.Add(enemy);
        }

        return enemies;
    }
}