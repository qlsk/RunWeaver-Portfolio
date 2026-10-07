using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// 범위 내 적을 당기고 방어력을 감소시키며 주기 피해를 준다.
public class SandstormArea : MonoBehaviour
{
    [FoldoutGroup("Settings"), SerializeField] private float _rangeCheckInterval = 0.1f;

    // Runtime settings
    private float _radius;
    private float _tickDamage;
    private float _tickInterval;
    private float _pullSpeed;
    private float _defenseMultiplier;

    // Runtime state
    private int _sourceId;
    private float _remainingDuration;
    private float _tickTimer;
    private float _rangeCheckTimer;
    private bool _isInitialized;

    // Affected targets
    private readonly Dictionary<EnemyHealth, EnemyStatusEffectController> _affectedEnemies = new();

    /// 영역 설정과 타이머를 초기화하고 현재 범위의 적에게 당김과 방어력 감소를 등록한다.
    public void Initialize(float radius, float duration, float tickDamage, float tickInterval, float pullSpeed, float defenseMultiplier)
    {
        ClearModifiers();

        _radius = radius;
        _remainingDuration = duration;
        _tickDamage = tickDamage;
        _tickInterval = tickInterval;
        _pullSpeed = pullSpeed;
        _defenseMultiplier = defenseMultiplier;
        _sourceId = GetInstanceID();
        _tickTimer = 0f;
        _rangeCheckTimer = 0f;
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

    /// 범위 검사와 주기 피해를 처리하고 수명이 끝나면 효과를 해제한 뒤 영역을 제거한다.
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
            RefreshModifiers();
        }

        while (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyTickDamage();
        }

        if (_remainingDuration <= 0f)
        {
            _isInitialized = false;
            ClearModifiers();
            Destroy(gameObject);
        }
    }

    /// 비활성화 시 이 영역이 등록한 당김과 방어력 감소만 해제한다.
    private void OnDisable()
    {
        ClearModifiers();
    }

    /// 파괴 시 남아 있는 효과 등록을 정리한다.
    private void OnDestroy()
    {
        ClearModifiers();
    }

    /// 진입한 적에게 효과를 등록하고 이탈한 적의 효과를 해제한다.
    private void RefreshModifiers()
    {
        HashSet<EnemyHealth> currentEnemies = FindDamageableEnemies();
        foreach (EnemyHealth enemy in currentEnemies)
        {
            if (_affectedEnemies.ContainsKey(enemy))
                continue;
            if (!enemy.TryGetComponent(out EnemyStatusEffectController statusEffects) || !statusEffects.isActiveAndEnabled)
                continue;

            enemy.AddDefenseModifier(_sourceId, _defenseMultiplier);
            statusEffects.AddPullModifier(_sourceId, transform.position, _pullSpeed);
            _affectedEnemies.Add(enemy, statusEffects);
        }

        EnemyHealth[] exitedEnemies = _affectedEnemies.Keys.Where(enemy =>
            !currentEnemies.Contains(enemy) || _affectedEnemies[enemy] == null || !_affectedEnemies[enemy].isActiveAndEnabled).ToArray();
        foreach (EnemyHealth enemy in exitedEnemies)
        {
            RemoveModifiers(enemy, _affectedEnemies[enemy]);
            _affectedEnemies.Remove(enemy);
        }
    }

    /// 피해 직전에 진입·이탈을 반영해 현재 범위의 적에게 방어력 감소가 적용된 틱 피해를 준다.
    private void ApplyTickDamage()
    {
        RefreshModifiers();
        foreach (EnemyHealth enemy in FindDamageableEnemies())
        {
            if (enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled)
                enemy.TakeDamage(_tickDamage);
        }
    }

    /// 보관한 모든 대상에서 이 영역의 효과를 해제하고 대상 목록을 비운다.
    private void ClearModifiers()
    {
        foreach (var affected in _affectedEnemies)
            RemoveModifiers(affected.Key, affected.Value);
        _affectedEnemies.Clear();
    }

    /// 파괴된 참조를 건너뛰면서 해당 적에게 등록한 두 효과를 해제한다.
    private void RemoveModifiers(EnemyHealth enemy, EnemyStatusEffectController statusEffects)
    {
        if (enemy != null)
            enemy.RemoveDefenseModifier(_sourceId);
        if (statusEffects != null)
            statusEffects.RemovePullModifier(_sourceId);
    }

    /// 현재 범위의 살아 있는 활성 상태 적을 콜라이더 중복 없이 조회한다.
    private HashSet<EnemyHealth> FindDamageableEnemies()
    {
        HashSet<EnemyHealth> enemies = new();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, _radius))
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
