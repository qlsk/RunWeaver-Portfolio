using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// 범위 내 적에게 연소와 중심으로의 당김을 적용하고 영역 종료 시 당김을 해제한다.
public class FirestormArea : MonoBehaviour
{
    [FoldoutGroup("Settings"), SerializeField] private float _rangeCheckInterval = 0.1f;

    [FoldoutGroup("References"), SerializeField] private ReactionAreaVisuals _areaVisuals;

    // Runtime settings
    private float _radius;
    private float _burnApplyInterval;
    private float _burnDamage;
    private float _burnTickInterval;
    private float _burnDuration;
    private float _pullSpeed;

    // Runtime state
    private int _sourceId;
    private float _remainingDuration;
    private float _rangeCheckTimer;
    private float _burnApplyTimer;
    private bool _isInitialized;

    // Affected targets
    private readonly HashSet<EnemyStatusEffectController> _affectedStatusEffects = new();

    /// 영역과 효과 설정을 초기화하고 활성 상태라면 범위 내 적에게 연소와 당김을 적용한다.
    public void Initialize(float radius, float duration, float burnApplyInterval, float burnDamage,
        float burnTickInterval, float burnDuration, float pullSpeed)
    {
        ClearPullModifiers();

        _radius = radius;
        _remainingDuration = duration;
        _burnApplyInterval = burnApplyInterval;
        _burnDamage = burnDamage;
        _burnTickInterval = burnTickInterval;
        _burnDuration = burnDuration;
        _pullSpeed = pullSpeed;
        _rangeCheckTimer = 0f;
        _burnApplyTimer = 0f;
        _sourceId = GetInstanceID();
        _isInitialized = true;
        if (_areaVisuals != null)
            _areaVisuals.Initialize(_radius);

        if (isActiveAndEnabled)
            RefreshPullModifiers();
    }

    /// 초기화된 영역이 다시 활성화되면 현재 범위의 적에게 연소와 당김을 등록한다.
    private void OnEnable()
    {
        if (!_isInitialized)
            return;

        _rangeCheckTimer = 0f;
        RefreshPullModifiers();
    }

    /// 범위와 연소 적용 주기를 처리하고 수명이 끝나면 당김을 해제한 뒤 영역을 제거한다.
    private void Update()
    {
        if (!_isInitialized)
            return;

        float elapsed = Mathf.Min(Time.deltaTime, _remainingDuration);
        _remainingDuration = Mathf.Max(0f, _remainingDuration - elapsed);
        _rangeCheckTimer += elapsed;
        _burnApplyTimer += elapsed;

        if (_rangeCheckTimer >= _rangeCheckInterval)
        {
            _rangeCheckTimer %= _rangeCheckInterval;
            RefreshPullModifiers();
        }

        if (_burnApplyTimer >= _burnApplyInterval)
        {
            _burnApplyTimer %= _burnApplyInterval;
            ApplyBurn();
        }

        if (_remainingDuration <= 0f)
        {
            _isInitialized = false;
            ClearPullModifiers();
            Destroy(gameObject);
        }
    }

    /// 영역 비활성화 시 자신이 등록한 당김을 모두 해제한다.
    private void OnDisable()
    {
        ClearPullModifiers();
    }

    /// 영역 파괴 시 남아 있는 당김 등록을 정리한다.
    private void OnDestroy()
    {
        ClearPullModifiers();
    }

    /// 새로 들어온 적에게 당김과 연소를 적용하고 범위를 벗어난 적의 당김을 해제한다.
    private void RefreshPullModifiers()
    {
        HashSet<EnemyStatusEffectController> currentStatusEffects = FindStatusEffects();
        IEnumerable<EnemyStatusEffectController> enteredStatusEffects =
            currentStatusEffects.Where(statusEffects => !_affectedStatusEffects.Contains(statusEffects));
        foreach (EnemyStatusEffectController statusEffects in enteredStatusEffects)
        {
            statusEffects.AddPullModifier(_sourceId, transform.position, _pullSpeed);
            statusEffects.ApplyBurn(_burnDamage, _burnTickInterval, _burnDuration);
        }

        IEnumerable<EnemyStatusEffectController> exitedStatusEffects = _affectedStatusEffects.Where(statusEffects =>
            statusEffects != null && !currentStatusEffects.Contains(statusEffects));
        foreach (EnemyStatusEffectController statusEffects in exitedStatusEffects)
        {
            statusEffects.RemovePullModifier(_sourceId);
        }

        _affectedStatusEffects.Clear();
        _affectedStatusEffects.UnionWith(currentStatusEffects);
    }

    /// 현재 범위 내 살아 있는 적들의 연소를 갱신한다.
    private void ApplyBurn()
    {
        foreach (EnemyStatusEffectController statusEffects in FindStatusEffects())
        {
            statusEffects.ApplyBurn(_burnDamage, _burnTickInterval, _burnDuration);
        }
    }

    /// 보관한 대상에서 이 영역의 당김만 해제하고 대상 목록을 비운다.
    private void ClearPullModifiers()
    {
        foreach (EnemyStatusEffectController statusEffects in _affectedStatusEffects.Where(statusEffects =>
                     statusEffects != null))
        {
            statusEffects.RemovePullModifier(_sourceId);
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
