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
    [SerializeField] private ParticleSystem _burnVfxPrefab;
    [SerializeField] private SpriteRenderer _burnVfxTarget;
    [SerializeField, Min(0.01f)] private float _burnVfxScale = 0.35f;
    [SerializeField] private Vector3 _burnVfxOffset;

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
    private ParticleSystem _burnVfxInstance;
    private ParticleSystemRenderer[] _burnVfxRenderers;

    // Slow state
    private float _slowRemaining;
    private float _areaMoveSpeedMultiplier = 1f;
    private readonly Dictionary<int, float> _moveSpeedModifiers = new();

    // Root state
    private readonly HashSet<int> _rootModifiers = new();

    // Petrify state
    private int _petrifySourceId;
    private float _petrifyRemaining;
    private float _petrifyMoveSpeedMultiplier;
    private float _petrifyDefenseMultiplier;
    private bool _petrifyRoots;
    private bool _hasPetrify;

    // Stagger state
    private float _staggerRemaining;

    // Knockback state
    private float _knockbackRemaining;
    private Vector2 _knockbackVelocity;

    // Pull state
    private readonly Dictionary<int, (Vector2 Center, float Speed)> _pullModifiers = new();

    // Push state
    private readonly Dictionary<int, (Vector2 Center, float Speed, Vector2 FallbackDirection)> _pushModifiers = new();

    public float MoveSpeedMultiplier => Mathf.Min(_slowRemaining > 0f ? _slowMoveSpeedMultiplier : 1f, _areaMoveSpeedMultiplier);
    public bool IsStaggered => _staggerRemaining > 0f;
    public bool IsRooted => _rootModifiers.Count > 0;
    public bool IsPulled => _pullModifiers.Count > 0;
    public bool IsPushed => _pushModifiers.Count > 0;
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

    /// 가장 가까운 밀어내기 중심에서 바깥으로 향하는 속도를 계산하고 중심에서는 대체 방향을 사용한다.
    public Vector2 PushVelocity
    {
        get
        {
            Vector2 position = transform.position;
            Vector2 direction = Vector2.zero;
            float speed = 0f;
            float nearestDistance = float.PositiveInfinity;

            foreach (var push in _pushModifiers.Values)
            {
                Vector2 offset = position - push.Center;
                float distance = offset.sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                direction = distance > 0f ? offset.normalized : push.FallbackDirection;
                speed = push.Speed;
            }

            return direction * speed;
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
            if (_burnRemaining <= 0f)
                StopBurnVisual(false);
        }

        _slowRemaining = Mathf.Max(_slowRemaining - deltaTime, 0f);
        _staggerRemaining = Mathf.Max(_staggerRemaining - deltaTime, 0f);
        _knockbackRemaining = Mathf.Max(_knockbackRemaining - deltaTime, 0f);
        if (_hasPetrify)
        {
            _petrifyRemaining = Mathf.Max(_petrifyRemaining - deltaTime, 0f);
            if (_petrifyRemaining <= 0f)
                ClearPetrify();
        }
    }

    /// 비활성화 시 화상 연출과 석화가 등록한 보정을 정리한다.
    private void OnDisable()
    {
        _burnRemaining = 0f;
        _burnTickTimer = 0f;
        StopBurnVisual(true);
        ClearPetrify();
    }

    /// 파괴 시 남아 있는 석화 등록을 정리한다.
    private void OnDestroy()
    {
        ClearPetrify();
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
                ApplyKnockback(bulletDirection, _knockbackSpeed, _knockbackDuration);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, null);
        }
    }

    /// 석화 시간을 갱신하고 재적용 시 강한 이동 제한과 방어력 감소를 유지한다.
    public void ApplyPetrify(float duration, float moveSpeedMultiplier, bool root, float defenseMultiplier)
    {
        if (!_hasPetrify)
        {
            _petrifySourceId = GetInstanceID();
            _petrifyMoveSpeedMultiplier = moveSpeedMultiplier;
            _petrifyDefenseMultiplier = defenseMultiplier;
            _petrifyRoots = root;
        }
        else
        {
            _petrifyMoveSpeedMultiplier = Mathf.Min(_petrifyMoveSpeedMultiplier, moveSpeedMultiplier);
            _petrifyDefenseMultiplier = Mathf.Min(_petrifyDefenseMultiplier, defenseMultiplier);
            _petrifyRoots |= root;
        }

        _hasPetrify = true;
        _petrifyRemaining = duration;
        AddMoveSpeedModifier(_petrifySourceId, _petrifyMoveSpeedMultiplier);
        if (_petrifyRoots)
            AddRootModifier(_petrifySourceId);
        _enemyHealth.AddDefenseModifier(_petrifySourceId, _petrifyDefenseMultiplier);
    }

    /// 지정한 방향과 속도로 일정 시간 넉백을 적용한다.
    public void ApplyKnockback(Vector2 direction, float speed, float duration)
    {
        _knockbackVelocity = direction.normalized * speed;
        _knockbackRemaining = duration;
    }

    /// 석화 출처만 해제하고 지속 상태를 초기화한다.
    private void ClearPetrify()
    {
        if (!_hasPetrify)
            return;

        RemoveMoveSpeedModifier(_petrifySourceId);
        RemoveRootModifier(_petrifySourceId);
        if (_enemyHealth != null)
            _enemyHealth.RemoveDefenseModifier(_petrifySourceId);
        _petrifyRemaining = 0f;
        _hasPetrify = false;
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
        if (_burnRemaining > 0f)
            PlayBurnVisual();
        else
            StopBurnVisual(false);
    }

    /// 화상 시작 시 하나의 이펙트를 생성하며 재적용은 기존 이펙트를 유지한다.
    private void PlayBurnVisual()
    {
        if (_burnVfxPrefab == null)
            return;

        if (_burnVfxInstance == null)
        {
            _burnVfxInstance = Instantiate(_burnVfxPrefab, transform, false);
            Vector3 center = _burnVfxTarget != null
                ? transform.InverseTransformPoint(_burnVfxTarget.bounds.center)
                : Vector3.zero;
            _burnVfxInstance.transform.localPosition = center + _burnVfxOffset;
            _burnVfxInstance.transform.localScale *= _burnVfxScale;
            _burnVfxRenderers = _burnVfxInstance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (ParticleSystem particles in _burnVfxInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main;
                main.stopAction = ParticleSystemStopAction.None;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }

        UpdateBurnSorting();
        if (!_burnVfxInstance.isEmitting)
            _burnVfxInstance.Play(true);
    }

    /// 종료 시 새 불꽃 방출을 멈추며 비활성화 시에는 남은 입자도 제거한다.
    private void StopBurnVisual(bool clear)
    {
        if (_burnVfxInstance != null)
            _burnVfxInstance.Stop(true, clear
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting);
    }

    private void LateUpdate()
    {
        UpdateBurnSorting();
    }

    /// 몬스터의 Y 정렬에 맞춰 불꽃을 몸체 바로 앞에 표시한다.
    private void UpdateBurnSorting()
    {
        if (_burnVfxTarget == null || _burnVfxRenderers == null)
            return;

        foreach (ParticleSystemRenderer renderer in _burnVfxRenderers)
        {
            renderer.sortingLayerID = _burnVfxTarget.sortingLayerID;
            renderer.sortingOrder = _burnVfxTarget.sortingOrder + 1;
        }
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

    /// 속박 출처를 등록하며 같은 출처의 중복 등록은 하나로 유지한다.
    public void AddRootModifier(int sourceId)
    {
        _rootModifiers.Add(sourceId);
    }

    /// 해당 출처의 속박을 해제하며 다른 출처의 속박은 유지한다.
    public void RemoveRootModifier(int sourceId)
    {
        _rootModifiers.Remove(sourceId);
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

    /// 출처별 밀어내기 중심, 속도와 중심에서 사용할 대체 방향을 등록하거나 갱신한다.
    public void AddPushModifier(int sourceId, Vector2 center, float speed, Vector2 fallbackDirection)
    {
        _pushModifiers[sourceId] = (center, speed, fallbackDirection.normalized);
    }

    /// 해당 출처의 밀어내기를 해제하며 다른 출처의 밀어내기는 유지한다.
    public void RemovePushModifier(int sourceId)
    {
        _pushModifiers.Remove(sourceId);
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
