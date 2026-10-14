using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// 범위 내 적을 중심으로 끌어당기고 주기 피해를 주며 영역 종료 시 당김을 해제한다.
public class TempestArea : MonoBehaviour
{
    [FoldoutGroup("Settings"), SerializeField] private float _rangeCheckInterval = 0.1f;

    [FoldoutGroup("References"), SerializeField] private ReactionAreaVisuals _areaVisuals;

    // Runtime settings
    private float _radius;
    private float _tickDamage;
    private float _tickInterval;
    private float _pullSpeed;

    // Runtime state
    private int _sourceId;
    private float _remainingDuration;
    private float _tickTimer;
    private float _rangeCheckTimer;
    private bool _isInitialized;

    // Affected targets
    private readonly HashSet<EnemyStatusEffectController> _affectedStatusEffects = new();

    /// 영역 설정과 타이머를 초기화하고 활성 상태라면 현재 범위의 적에게 당김을 등록한다.
    public void Initialize(float radius, float duration, float tickDamage, float tickInterval, float pullSpeed)
    {
        ClearPullModifiers();

        _radius = radius;
        _remainingDuration = duration;
        _tickDamage = tickDamage;
        _tickInterval = tickInterval;
        _pullSpeed = pullSpeed;
        _sourceId = GetInstanceID();
        _tickTimer = 0f;
        _rangeCheckTimer = 0f;
        _isInitialized = true;
        if (_areaVisuals != null)
            _areaVisuals.Initialize(_radius);

        if (isActiveAndEnabled)
            RefreshPullModifiers();
    }

    /// 초기화된 영역이 다시 활성화되면 현재 범위의 적에게 당김을 등록한다.
    private void OnEnable()
    {
        if (!_isInitialized)
            return;

        _rangeCheckTimer = 0f;
        RefreshPullModifiers();
    }

    /// 범위 검사와 주기 피해를 처리하고 수명이 끝나면 당김을 해제한 뒤 영역을 제거한다.
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
            RefreshPullModifiers();
        }

        while (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyTickDamage();
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

    /// 새로 들어온 적에게 당김을 등록하고 범위를 벗어난 적에게서 자신의 당김만 제거한다.
    private void RefreshPullModifiers()
    {
        HashSet<EnemyStatusEffectController> currentStatusEffects = new();
        foreach (EnemyHealth enemy in FindDamageableEnemies())
        {
            if (!enemy.TryGetComponent(out EnemyStatusEffectController statusEffects) || !statusEffects.isActiveAndEnabled)
                continue;

            currentStatusEffects.Add(statusEffects);
            if (!_affectedStatusEffects.Contains(statusEffects))
                statusEffects.AddPullModifier(_sourceId, transform.position, _pullSpeed);
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

    /// 현재 범위 내 살아 있는 적에게 중복 없이 한 주기의 피해를 적용한다.
    private void ApplyTickDamage()
    {
        foreach (EnemyHealth enemy in FindDamageableEnemies())
        {
            if (enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled)
                enemy.TakeDamage(_tickDamage);
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

    /// 현재 범위 내 살아 있는 활성 상태의 적을 콜라이더 중복 없이 조회한다.
    private HashSet<EnemyHealth> FindDamageableEnemies()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _radius);
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
