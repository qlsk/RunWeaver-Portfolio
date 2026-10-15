using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatusEffectController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyHealth _enemyHealth;

    [Header("Burn")]
    [SerializeField] private float _burnDuration = 3f;
    [SerializeField] private float _burnInterval = 1f;
    [SerializeField] private float _burnDamageMultiplier = 0.2f;

    [Header("Slow")]
    [SerializeField] private float _slowDuration = 3f;
    [SerializeField] private float _slowMoveSpeedMultiplier = 0.5f;

    [Header("Stagger")]
    [SerializeField] private float _staggerDuration = 0.5f;

    [Header("Knockback")]
    [SerializeField] private float _knockbackDuration = 0.2f;
    [SerializeField] private float _knockbackSpeed = 5f;

    // Burn state
    private float _burnDamage;
    private float _activeBurnInterval;
    private float _burnRemaining;
    private float _burnTickTimer;

    // Slow state
    private float _slowRemaining;
    private float _areaMoveSpeedMultiplier = 1f;
    private readonly Dictionary<int, float> _moveSpeedModifiers = new();

    // Stagger state
    private float _staggerRemaining;

    // Knockback state
    private float _knockbackRemaining;
    private Vector2 _knockbackVelocity;

    // Pull state
    private readonly Dictionary<int, (Vector2 Center, float Speed)> _pullModifiers = new();

    public float MoveSpeedMultiplier => Mathf.Min(_slowRemaining > 0f ? _slowMoveSpeedMultiplier : 1f, _areaMoveSpeedMultiplier);
    public bool IsStaggered => _staggerRemaining > 0f;
    public bool IsPulled => _pullModifiers.Count > 0;
    public Vector2 KnockbackVelocity => _knockbackRemaining > 0f ? _knockbackVelocity : Vector2.zero;
    public Vector2 PullVelocity
    {
        get
        {
            Vector2 position = transform.position;
            Vector2 offset = Vector2.zero;
            float speed = 0f;
            float nearestDistance = float.PositiveInfinity;

            foreach (var pull in _pullModifiers.Values)
            {
                Vector2 candidateOffset = pull.Center - position;
                float distance = candidateOffset.sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                offset = candidateOffset;
                speed = pull.Speed;
            }

            float remainingDistance = offset.magnitude;
            float pullSpeed = Mathf.Min(speed * Mathf.Min(1f, remainingDistance), remainingDistance / Time.fixedDeltaTime);
            return offset.normalized * pullSpeed;
        }
    }

    /// 연소의 주기 피해를 처리하고 각 상태 효과의 남은 시간을 갱신한다.
    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (!(_burnRemaining <= 0f))
        {
            _burnTickTimer += Mathf.Min(deltaTime, _burnRemaining);
            _burnRemaining = Mathf.Max(_burnRemaining - deltaTime, 0f);
            while (_burnTickTimer >= _activeBurnInterval)
            {
                _burnTickTimer -= _activeBurnInterval;
                _enemyHealth.TakeDamage(_burnDamage);
            }
        }

        _slowRemaining = Mathf.Max(_slowRemaining - deltaTime, 0f);
        _staggerRemaining = Mathf.Max(_staggerRemaining - deltaTime, 0f);
        _knockbackRemaining = Mathf.Max(_knockbackRemaining - deltaTime, 0f);
    }

    /// 단일 원소에 따라 연소, 감속, 경직 또는 넉백을 적용한다.
    public void ApplyElementEffect(ReactionElement element, float bulletDamage, Vector2 bulletDirection)
    {
        switch (element)
        {
            case ReactionElement.Fire:
                ApplyBurn(bulletDamage * _burnDamageMultiplier, _burnInterval, _burnDuration);
                break;
            case ReactionElement.Water:
                _slowRemaining = _slowDuration;
                break;
            case ReactionElement.Earth:
                _staggerRemaining = _staggerDuration;
                break;
            case ReactionElement.Wind:
                _knockbackVelocity = bulletDirection.normalized * _knockbackSpeed;
                _knockbackRemaining = _knockbackDuration;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, null);
        }
    }

    /// 연소 시간을 갱신하고 재적용 시 피해 타이머를 유지하면서 더 높은 초당 피해 설정을 채택한다.
    public void ApplyBurn(float tickDamage, float tickInterval, float duration)
    {
        if (_burnRemaining <= 0f)
        {
            _burnDamage = tickDamage;
            _activeBurnInterval = tickInterval;
            _burnTickTimer = 0f;
        }
        else if (tickDamage / tickInterval > _burnDamage / _activeBurnInterval)
        {
            _burnDamage = tickDamage;
            _activeBurnInterval = tickInterval;
        }

        _burnRemaining = duration;
    }

    /// 출처별 이동 속도 배율을 등록하거나 갱신하고 적용할 감속 배율을 계산한다.
    public void AddMoveSpeedModifier(int sourceId, float multiplier)
    {
        _moveSpeedModifiers[sourceId] = multiplier;
        RefreshAreaMoveSpeedMultiplier();
    }

    /// 해당 출처의 이동 속도 보정을 해제하고 남은 보정의 배율을 계산한다.
    public void RemoveMoveSpeedModifier(int sourceId)
    {
        _moveSpeedModifiers.Remove(sourceId);
        RefreshAreaMoveSpeedMultiplier();
    }

    /// 출처별 당김 중심과 속도를 등록하거나 갱신한다.
    public void AddPullModifier(int sourceId, Vector2 center, float speed)
    {
        _pullModifiers[sourceId] = (center, speed);
    }

    /// 해당 출처의 당김을 해제하며 다른 출처의 당김은 유지한다.
    public void RemovePullModifier(int sourceId)
    {
        _pullModifiers.Remove(sourceId);
    }

    /// 등록된 영역 보정 중 가장 낮은 이동 속도 배율을 선택하고 보정이 없으면 1로 복구한다.
    private void RefreshAreaMoveSpeedMultiplier()
    {
        _areaMoveSpeedMultiplier = 1f;
        foreach (float multiplier in _moveSpeedModifiers.Values)
        {
            _areaMoveSpeedMultiplier = Mathf.Min(_areaMoveSpeedMultiplier, multiplier);
        }
    }
}
