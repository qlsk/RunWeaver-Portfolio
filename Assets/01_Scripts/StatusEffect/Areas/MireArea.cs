using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// 범위 내 적을 감속하고 연속 노출 시간이 기준에 도달하면 영역을 벗어날 때까지 속박한다.
public class MireArea : MonoBehaviour
{
    [FoldoutGroup("Settings"), SerializeField] private float _rangeCheckInterval = 0.1f;

    // Runtime settings
    private float _radius;
    private float _moveSpeedMultiplier;
    private float _rootExposureDuration;

    // Runtime state
    private int _sourceId;
    private float _remainingDuration;
    private float _rangeCheckTimer;
    private bool _isInitialized;

    // Affected targets
    private readonly Dictionary<EnemyStatusEffectController, float> _exposureDurations = new();

    /// 영역 설정과 노출 기록을 초기화하고 활성 상태라면 현재 범위의 적에게 감속을 적용한다.
    public void Initialize(float radius, float duration, float moveSpeedMultiplier, float rootExposureDuration)
    {
        ClearModifiers();

        _radius = radius;
        _remainingDuration = duration;
        _moveSpeedMultiplier = moveSpeedMultiplier;
        _rootExposureDuration = rootExposureDuration;
        _rangeCheckTimer = 0f;
        _sourceId = GetInstanceID();
        _isInitialized = true;

        if (isActiveAndEnabled)
            RefreshModifiers(0f);
    }

    /// 초기화된 영역이 다시 활성화되면 새로운 노출 기록으로 감속을 등록한다.
    private void OnEnable()
    {
        if (!_isInitialized)
            return;

        _rangeCheckTimer = 0f;
        RefreshModifiers(0f);
    }

    /// 범위 검사 사이의 실제 경과 시간을 노출 시간에 반영하고 수명이 끝나면 영역을 제거한다.
    private void Update()
    {
        if (!_isInitialized)
            return;

        float elapsed = Mathf.Min(Time.deltaTime, _remainingDuration);
        _remainingDuration = Mathf.Max(0f, _remainingDuration - elapsed);
        _rangeCheckTimer += elapsed;

        if (_remainingDuration <= 0f)
        {
            _isInitialized = false;
            ClearModifiers();
            Destroy(gameObject);
            return;
        }

        if (_rangeCheckTimer >= _rangeCheckInterval)
        {
            float exposureElapsed = _rangeCheckTimer;
            _rangeCheckTimer = 0f;
            RefreshModifiers(exposureElapsed);
        }
    }

    /// 영역 비활성화 시 자신이 등록한 효과와 노출 기록을 모두 해제한다.
    private void OnDisable()
    {
        ClearModifiers();
    }

    /// 영역 파괴 시 남아 있는 감속과 속박 등록을 정리한다.
    private void OnDestroy()
    {
        ClearModifiers();
    }

    /// 진입한 적을 감속하고 계속 머무른 적의 노출 시간을 누적하며 이탈한 적의 효과를 해제한다.
    private void RefreshModifiers(float elapsed)
    {
        HashSet<EnemyStatusEffectController> currentStatusEffects = FindStatusEffects();
        foreach (EnemyStatusEffectController statusEffects in currentStatusEffects)
        {
            if (!_exposureDurations.TryGetValue(statusEffects, out float exposureDuration))
            {
                statusEffects.AddMoveSpeedModifier(_sourceId, _moveSpeedMultiplier);
                _exposureDurations.Add(statusEffects, 0f);
                continue;
            }

            if (exposureDuration >= _rootExposureDuration)
                continue;

            exposureDuration = Mathf.Min(exposureDuration + elapsed, _rootExposureDuration);
            _exposureDurations[statusEffects] = exposureDuration;
            if (exposureDuration >= _rootExposureDuration)
                statusEffects.AddRootModifier(_sourceId);
        }

        EnemyStatusEffectController[] exitedStatusEffects = _exposureDurations.Keys
            .Where(statusEffects => !currentStatusEffects.Contains(statusEffects)).ToArray();
        foreach (EnemyStatusEffectController statusEffects in exitedStatusEffects)
        {
            if (statusEffects != null)
            {
                statusEffects.RemoveMoveSpeedModifier(_sourceId);
                statusEffects.RemoveRootModifier(_sourceId);
            }

            _exposureDurations.Remove(statusEffects);
        }
    }

    /// 보관한 대상에서 이 영역의 효과만 해제하고 노출 기록을 비운다.
    private void ClearModifiers()
    {
        foreach (EnemyStatusEffectController statusEffects in _exposureDurations.Keys.Where(statusEffects =>
                     statusEffects != null))
        {
            statusEffects.RemoveMoveSpeedModifier(_sourceId);
            statusEffects.RemoveRootModifier(_sourceId);
        }

        _exposureDurations.Clear();
    }

    /// 범위 내 살아 있는 적의 활성 상태 효과 컴포넌트를 중복 없이 조회한다.
    private HashSet<EnemyStatusEffectController> FindStatusEffects()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _radius);
        HashSet<EnemyStatusEffectController> statusEffects = new();
        foreach (Collider2D collider in colliders)
        {
            if (!collider.TryGetComponent(out EnemyHealth enemy))
                continue;
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled)
                continue;
            if (!enemy.TryGetComponent(out EnemyStatusEffectController effects) || !effects.isActiveAndEnabled)
                continue;

            statusEffects.Add(effects);
        }

        return statusEffects;
    }
}
