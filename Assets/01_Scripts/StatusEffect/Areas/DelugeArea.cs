using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// 범위 내 적에게 감속과 밀어내기를 적용하고 영역을 벗어나거나 종료되면 해제한다.
public class DelugeArea : MonoBehaviour
{
    [FoldoutGroup("Settings"), SerializeField] private float _rangeCheckInterval = 0.1f;

    // Runtime settings
    private float _radius;
    private float _moveSpeedMultiplier;
    private float _pushSpeed;
    private Vector2 _fallbackDirection;

    // Runtime state
    private int _sourceId;
    private float _remainingDuration;
    private float _rangeCheckTimer;
    private bool _isInitialized;

    // Affected targets
    private readonly HashSet<EnemyStatusEffectController> _affectedStatusEffects = new();

    /// 영역 설정을 초기화하고 활성 상태라면 범위 내 적에게 감속과 밀어내기를 등록한다.
    public void Initialize(float radius, float duration, float moveSpeedMultiplier, float pushSpeed,
        Vector2 fallbackDirection)
    {
        ClearModifiers();

        _radius = radius;
        _remainingDuration = duration;
        _moveSpeedMultiplier = moveSpeedMultiplier;
        _pushSpeed = pushSpeed;
        _fallbackDirection = fallbackDirection.normalized;
        _rangeCheckTimer = 0f;
        _sourceId = GetInstanceID();
        _isInitialized = true;

        if (isActiveAndEnabled)
            RefreshModifiers();
    }

    /// 초기화된 영역이 다시 활성화되면 현재 범위의 적에게 효과를 등록한다.
    private void OnEnable()
    {
        if (!_isInitialized)
            return;

        _rangeCheckTimer = 0f;
        RefreshModifiers();
    }

    /// 일정 간격으로 범위 내 대상을 갱신하고 수명이 끝나면 효과를 해제한 뒤 영역을 제거한다.
    private void Update()
    {
        if (!_isInitialized)
            return;

        float elapsed = Mathf.Min(Time.deltaTime, _remainingDuration);
        _remainingDuration = Mathf.Max(0f, _remainingDuration - elapsed);
        _rangeCheckTimer += elapsed;

        if (_rangeCheckTimer >= _rangeCheckInterval)
        {
            _rangeCheckTimer %= _rangeCheckInterval;
            RefreshModifiers();
        }

        if (_remainingDuration <= 0f)
        {
            _isInitialized = false;
            ClearModifiers();
            Destroy(gameObject);
        }
    }

    /// 영역 비활성화 시 자신이 등록한 감속과 밀어내기를 모두 해제한다.
    private void OnDisable()
    {
        ClearModifiers();
    }

    /// 영역 파괴 시 남아 있는 감속과 밀어내기 등록을 정리한다.
    private void OnDestroy()
    {
        ClearModifiers();
    }

    /// 새로 들어온 적에게 효과를 등록하고 범위를 벗어난 적에게서 자신의 효과만 해제한다.
    private void RefreshModifiers()
    {
        HashSet<EnemyStatusEffectController> currentStatusEffects = FindStatusEffects();
        IEnumerable<EnemyStatusEffectController> enteredStatusEffects =
            currentStatusEffects.Where(statusEffects => !_affectedStatusEffects.Contains(statusEffects));
        foreach (EnemyStatusEffectController statusEffects in enteredStatusEffects)
        {
            statusEffects.AddMoveSpeedModifier(_sourceId, _moveSpeedMultiplier);
            statusEffects.AddPushModifier(_sourceId, transform.position, _pushSpeed, _fallbackDirection);
        }

        IEnumerable<EnemyStatusEffectController> exitedStatusEffects = _affectedStatusEffects.Where(statusEffects =>
            statusEffects != null && !currentStatusEffects.Contains(statusEffects));
        foreach (EnemyStatusEffectController statusEffects in exitedStatusEffects)
        {
            statusEffects.RemoveMoveSpeedModifier(_sourceId);
            statusEffects.RemovePushModifier(_sourceId);
        }

        _affectedStatusEffects.Clear();
        _affectedStatusEffects.UnionWith(currentStatusEffects);
    }

    /// 보관한 대상에서 이 영역의 효과만 해제하고 대상 목록을 비운다.
    private void ClearModifiers()
    {
        foreach (EnemyStatusEffectController statusEffects in _affectedStatusEffects.Where(statusEffects =>
                     statusEffects != null))
        {
            statusEffects.RemoveMoveSpeedModifier(_sourceId);
            statusEffects.RemovePushModifier(_sourceId);
        }

        _affectedStatusEffects.Clear();
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
