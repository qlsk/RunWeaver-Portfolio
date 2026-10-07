using System;
using UnityEngine;

/// <summary>원소 상태이상과 회복 스킬용 해제 기능을 관리한다.</summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerStatusEffectController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth _playerHealth;

    [Header("Burn")]
    [SerializeField, Min(0f)] private float _burnDuration = 3f;
    [SerializeField, Min(0.01f)] private float _burnInterval = 1f;
    [SerializeField, Min(0f)] private float _burnDamageMultiplier = 0.2f;

    [Header("Slow")]
    [SerializeField, Min(0f)] private float _slowDuration = 3f;
    [SerializeField, Range(0f, 1f)] private float _slowMoveSpeedMultiplier = 0.5f;

    [Header("Attack Slow")]
    [SerializeField, Min(0f)] private float _attackSlowDuration = 3f;
    [SerializeField, Min(1f)] private float _attackIntervalMultiplier = 1.5f;

    [Header("Stagger")]
    [SerializeField, Min(0f)] private float _staggerDuration = 0.5f;

    private float _burnRemaining;
    private float _burnTickTimer;
    private float _burnDamage;
    private float _slowRemaining;
    private float _attackSlowRemaining;
    private float _staggerRemaining;
    private float _immuneUntil;

    public float MoveSpeedMultiplier => isActiveAndEnabled && _slowRemaining > 0f
        ? Mathf.Clamp01(_slowMoveSpeedMultiplier) : 1f;
    public float AttackIntervalMultiplier => isActiveAndEnabled && _attackSlowRemaining > 0f
        ? Mathf.Max(1f, _attackIntervalMultiplier) : 1f;
    public bool IsStaggered => isActiveAndEnabled && _staggerRemaining > 0f;

    private void Awake()
    {
        if (_playerHealth == null)
        {
            _playerHealth = GetComponent<PlayerHealth>();
        }
    }

    /// <summary>재적용 시 지속 시간을 갱신하고 화상은 높은 피해를 유지한다.</summary>
    public void ApplyElementEffect(ReactionElement element, float damage)
    {
        if (!isActiveAndEnabled || _playerHealth.IsDead)
            return;

        // 보상 특성용 면역 (clease_01). 면역 중에는 새 상태이상을 받지 않는다.
        if (Time.time < _immuneUntil)
            return;

        switch (element)
        {
            case ReactionElement.Fire:
                float burnDamage = Mathf.Max(0f, damage) * Mathf.Max(0f, _burnDamageMultiplier);
                if (_burnRemaining > 0f)
                {
                    _burnDamage = Mathf.Max(_burnDamage, burnDamage);
                }
                else
                {
                    _burnDamage = burnDamage;
                    _burnTickTimer = 0f;
                }
                _burnRemaining = Mathf.Max(0f, _burnDuration);
                break;
            case ReactionElement.Water:
                _slowRemaining = Mathf.Max(0f, _slowDuration);
                break;
            case ReactionElement.Wind:
                _attackSlowRemaining = Mathf.Max(0f, _attackSlowDuration);
                break;
            case ReactionElement.Earth:
                _staggerRemaining = Mathf.Max(0f, _staggerDuration);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, null);
        }
    }

    public float GetRemainingDuration(ReactionElement element)
    {
        return element switch
        {
            ReactionElement.Fire => _burnRemaining,
            ReactionElement.Water => _slowRemaining,
            ReactionElement.Wind => _attackSlowRemaining,
            ReactionElement.Earth => _staggerRemaining,
            _ => throw new ArgumentOutOfRangeException(nameof(element), element, null)
        };
    }

    public bool HasEffect(ReactionElement element) => GetRemainingDuration(element) > 0f;

    /// <summary>지정한 상태이상을 해제하고 실제 해제 여부를 반환한다.</summary>
    public bool RemoveEffect(ReactionElement element)
    {
        bool hadEffect = HasEffect(element);
        switch (element)
        {
            case ReactionElement.Fire:
                _burnRemaining = 0f;
                _burnTickTimer = 0f;
                _burnDamage = 0f;
                break;
            case ReactionElement.Water:
                _slowRemaining = 0f;
                break;
            case ReactionElement.Wind:
                _attackSlowRemaining = 0f;
                break;
            case ReactionElement.Earth:
                _staggerRemaining = 0f;
                break;
        }
        return hadEffect;
    }

    /// <summary>보상 특성용: 새 해로운 상태에 면역. duration초 동안 ApplyElementEffect를 무시한다.</summary>
    public void GrantImmunity(float duration)
    {
        _immuneUntil = Mathf.Max(_immuneUntil, Time.time + Mathf.Max(0f, duration));
    }

    /// <summary>모든 상태이상을 해제하고 해제한 개수를 반환한다.</summary>
    public int ClearAllEffects()
    {
        int cleared = 0;
        if (RemoveEffect(ReactionElement.Fire)) cleared++;
        if (RemoveEffect(ReactionElement.Water)) cleared++;
        if (RemoveEffect(ReactionElement.Wind)) cleared++;
        if (RemoveEffect(ReactionElement.Earth)) cleared++;
        return cleared;
    }

    private void OnDisable() => ClearAllEffects();

    private void Update()
    {
        if (_playerHealth.IsDead)
        {
            ClearAllEffects();
            return;
        }

        float deltaTime = Time.deltaTime;
        if (_burnRemaining > 0f)
        {
            _burnTickTimer += Mathf.Min(deltaTime, _burnRemaining);
            _burnRemaining = Mathf.Max(0f, _burnRemaining - deltaTime);
            float interval = Mathf.Max(0.01f, _burnInterval);
            while (_burnTickTimer >= interval)
            {
                _burnTickTimer -= interval;
                _playerHealth.TakeDamageOverTime(_burnDamage);
                if (_playerHealth.IsDead || !isActiveAndEnabled)
                {
                    ClearAllEffects();
                    return;
                }
            }
            if (_burnRemaining <= 0f)
            {
                RemoveEffect(ReactionElement.Fire);
            }
        }

        _slowRemaining = Mathf.Max(0f, _slowRemaining - deltaTime);
        _attackSlowRemaining = Mathf.Max(0f, _attackSlowRemaining - deltaTime);
        _staggerRemaining = Mathf.Max(0f, _staggerRemaining - deltaTime);
    }
}
