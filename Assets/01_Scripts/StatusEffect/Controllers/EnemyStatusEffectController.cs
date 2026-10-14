using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyStatusEffectController : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private EnemyHealth _enemyHealth;

    [FoldoutGroup("Burn Settings"), SerializeField] private float _burnDuration = 3f;
    [FoldoutGroup("Burn Settings"), SerializeField] private float _burnInterval = 1f;
    [FoldoutGroup("Burn Settings"), SerializeField] private float _burnDamageMultiplier = 0.2f;
    [FoldoutGroup("Burn VFX"), SerializeField] private GameObject _burnVfxPrefab;
    [FoldoutGroup("Burn VFX"), SerializeField] private SpriteRenderer _burnVfxTarget;
    [FoldoutGroup("Burn VFX"), SerializeField, Min(0.01f)] private float _burnVfxScale = 0.35f;
    [FoldoutGroup("Burn VFX"), SerializeField] private Vector3 _burnVfxOffset;

    [FoldoutGroup("Slow Settings"), SerializeField] private float _slowDuration = 3f;
    [FoldoutGroup("Slow Settings"), SerializeField] private float _slowMoveSpeedMultiplier = 0.5f;
    [FoldoutGroup("Slow VFX"), SerializeField] private GameObject _slowVfxPrefab;
    [FoldoutGroup("Slow VFX"), SerializeField] private SpriteRenderer _slowVfxTarget;
    [FoldoutGroup("Slow VFX"), SerializeField, Min(0.01f)] private float _slowVfxScale = 0.35f;
    [FoldoutGroup("Slow VFX"), SerializeField] private Vector3 _slowVfxOffset;

    [FoldoutGroup("Stagger Settings"), SerializeField] private float _staggerDuration = 0.5f;
    [FoldoutGroup("Stagger VFX"), SerializeField] private GameObject _staggerVfxPrefab;
    [FoldoutGroup("Stagger VFX"), SerializeField] private SpriteRenderer _staggerVfxTarget;
    [FoldoutGroup("Stagger VFX"), SerializeField, Min(0.01f)] private float _staggerVfxScale = 0.35f;
    [FoldoutGroup("Stagger VFX"), SerializeField] private Vector3 _staggerVfxOffset;

    [FoldoutGroup("Knockback Settings"), SerializeField] private float _knockbackDuration = 0.2f;
    [FoldoutGroup("Knockback Settings"), SerializeField] private float _knockbackSpeed = 5f;
    [FoldoutGroup("Knockback VFX"), SerializeField] private GameObject _knockbackVfxPrefab;
    [FoldoutGroup("Knockback VFX"), SerializeField] private SpriteRenderer _knockbackVfxTarget;
    [FoldoutGroup("Knockback VFX"), SerializeField, Min(0.01f)] private float _knockbackVfxScale = 0.35f;
    [FoldoutGroup("Knockback VFX"), SerializeField] private Vector3 _knockbackVfxOffset;
    [FoldoutGroup("Knockback VFX"), SerializeField] private float _knockbackVfxRotationOffset = 90f;

    [FoldoutGroup("Death VFX"), SerializeField, Min(0.1f)] private float _detachedVfxLifetime = 1.2f;

    // 경직 연출 클립(Effect_Stagger)의 정규화 시간: 16프레임에서 가시가 모두 올라와 있고, 24프레임부터 내려간다.
    private const float StaggerVfxHoldTime = 16f / 32f;
    private const float StaggerVfxRetractTime = 24f / 32f;

    // Burn state
    private float _burnDamage;
    private float _activeBurnInterval;
    private float _burnRemaining;
    private float _burnTickTimer;
    private GameObject _burnVfxInstance;
    private SpriteRenderer[] _burnVfxRenderers;

    // Slow state
    private float _slowRemaining;
    private GameObject _slowVfxInstance;
    private SpriteRenderer[] _slowVfxRenderers;
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
    private GameObject _staggerVfxInstance;
    private SpriteRenderer[] _staggerVfxRenderers;
    private Animator _staggerVfxAnimator;
    private bool _isStaggerVfxRetracting;

    // Knockback state
    private float _knockbackRemaining;
    private Vector2 _knockbackVelocity;
    private GameObject _knockbackVfxInstance;
    private SpriteRenderer[] _knockbackVfxRenderers;
    private Animator _knockbackVfxAnimator;

    // Pull state
    private readonly Dictionary<int, (Vector2 Center, float Speed)> _pullModifiers = new();

    // Push state
    private readonly Dictionary<int, (Vector2 Center, float Speed, Vector2 FallbackDirection)> _pushModifiers = new();

    public float MoveSpeedMultiplier =>
        Mathf.Min(_slowRemaining > 0f ? _slowMoveSpeedMultiplier : 1f, _areaMoveSpeedMultiplier);
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
            float pullSpeed = Mathf.Min(speed * Mathf.Min(1f, remainingDistance),
                remainingDistance / Time.fixedDeltaTime);
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
                StopBurnVisual();
        }

        _slowRemaining = Mathf.Max(_slowRemaining - deltaTime, 0f);
        if (_slowRemaining <= 0f)
            StopSlowVisual();
        _staggerRemaining = Mathf.Max(_staggerRemaining - deltaTime, 0f);
        // 석화 중이거나 석화가 풀려 가시가 내려가는 중에는 경직 연출을 유지한다.
        if (_staggerRemaining <= 0f && !_hasPetrify && !_isStaggerVfxRetracting)
            StopStaggerVisual();
        _knockbackRemaining = Mathf.Max(_knockbackRemaining - deltaTime, 0f);
        if (_knockbackRemaining <= 0f)
            StopKnockbackVisual();
        if (_hasPetrify)
        {
            _petrifyRemaining = Mathf.Max(_petrifyRemaining - deltaTime, 0f);
            if (_petrifyRemaining <= 0f)
            {
                ClearPetrify();
                RetractStaggerVisual();
            }
            else
            {
                HoldStaggerVisualWhilePetrified();
            }
        }

        if (_isStaggerVfxRetracting && IsStaggerVfxFinished())
            StopStaggerVisual();
    }

    /// 비활성화 시 단일 원소 연출과 석화가 등록한 보정을 정리한다.
    private void OnDisable()
    {
        _burnRemaining = 0f;
        _burnTickTimer = 0f;
        StopBurnVisual();
        _slowRemaining = 0f;
        StopSlowVisual();
        _staggerRemaining = 0f;
        StopStaggerVisual();
        _knockbackRemaining = 0f;
        StopKnockbackVisual();
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
                if (_slowRemaining > 0f)
                    PlaySlowVisual();
                else
                    StopSlowVisual();
                break;
            case ReactionElement.Earth:
                _staggerRemaining = _staggerDuration;
                // 석화 중에는 올라와 있는 가시를 다시 처음부터 재생하지 않는다.
                if (_hasPetrify)
                    break;
                if (_staggerRemaining > 0f)
                    PlayStaggerVisual();
                else
                    StopStaggerVisual();
                break;
            case ReactionElement.Wind:
                ApplyKnockback(bulletDirection, _knockbackSpeed, _knockbackDuration);
                if (_knockbackRemaining > 0f)
                    PlayKnockbackVisual(bulletDirection);
                else
                    StopKnockbackVisual();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, null);
        }
    }

    /// <summary>
    /// 적이 이미 죽었을 때 단일 원소 연출만 몬스터와 분리해 그 자리에 재생한다.
    /// 몬스터가 사라져도 연출은 남고, 일정 시간 뒤 스스로 제거된다. 상태 효과는 적용하지 않는다.
    /// </summary>
    public void PlayElementVisualDetached(ReactionElement element, Vector2 bulletDirection)
    {
        GameObject prefab;
        SpriteRenderer target;
        float scale;
        Vector3 offset;
        int order;
        switch (element)
        {
            case ReactionElement.Fire:
                prefab = _burnVfxPrefab; target = _burnVfxTarget; scale = _burnVfxScale; offset = _burnVfxOffset; order = 1;
                break;
            case ReactionElement.Water:
                prefab = _slowVfxPrefab; target = _slowVfxTarget; scale = _slowVfxScale; offset = _slowVfxOffset; order = 2;
                break;
            case ReactionElement.Earth:
                prefab = _staggerVfxPrefab; target = _staggerVfxTarget; scale = _staggerVfxScale; offset = _staggerVfxOffset; order = 3;
                break;
            case ReactionElement.Wind:
                prefab = _knockbackVfxPrefab; target = _knockbackVfxTarget; scale = _knockbackVfxScale; offset = _knockbackVfxOffset; order = 4;
                break;
            default:
                return;
        }

        if (prefab == null)
            return;

        // 사망 처리로 몸체가 꺼졌으면 bounds가 비므로 몸체 위치를 쓴다.
        bool hasBounds = target != null && target.enabled && target.gameObject.activeInHierarchy;
        Vector3 center = hasBounds ? transform.InverseTransformPoint(target.bounds.center)
            : target != null ? transform.InverseTransformPoint(target.transform.position) : Vector3.zero;
        Vector3 position = transform.TransformPoint(center + offset);
        Quaternion rotation = prefab.transform.rotation;
        if (element == ReactionElement.Wind && bulletDirection != Vector2.zero)
        {
            float angle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg +
                          _knockbackVfxRotationOffset;
            rotation = Quaternion.Euler(0f, 0f, angle);
        }

        GameObject instance = Instantiate(prefab, position, rotation);
        Vector3 parentScale = transform.lossyScale;
        instance.transform.localScale = Vector3.Scale(prefab.transform.localScale, parentScale) * scale;
        if (target != null)
        {
            foreach (SpriteRenderer renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingLayerID = target.sortingLayerID;
                renderer.sortingOrder = target.sortingOrder + order;
            }
        }

        instance.SetActive(true);
        if (instance.TryGetComponent(out Animator animator))
            animator.Play(0, 0, 0f);
        Destroy(instance, _detachedVfxLifetime);
    }

    /// 석화 시간을 갱신하고 재적용 시 강한 이동 제한과 방어력 감소를 유지한다.
    public void ApplyPetrify(float duration, float moveSpeedMultiplier, bool root, float defenseMultiplier)
    {
        bool wasPetrified = _hasPetrify;
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
        // 새로 석화되면 가시가 올라오는 연출을 처음부터 재생한다. 이미 석화 중이면 올라와 있는 가시를 그대로 둔다.
        if (!wasPetrified)
            PlayStaggerVisual();
    }

    /// 지정한 방향과 속도로 일정 시간 넉백을 적용한다.
    public void ApplyKnockback(Vector2 direction, float speed, float duration)
    {
        _knockbackVelocity = direction.normalized * speed;
        _knockbackRemaining = duration;
    }

    /// 보상 특성(shield_04 파열의 고리)용: 넉백을 걸고 바람 넉백 연출을 함께 재생한다.
    public void ApplyKnockbackWithVisual(Vector2 direction, float speed, float duration)
    {
        ApplyKnockback(direction, speed, duration);
        if (_knockbackRemaining > 0f)
            PlayKnockbackVisual(direction);
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
            StopBurnVisual();
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
            _burnVfxRenderers = _burnVfxInstance.GetComponentsInChildren<SpriteRenderer>(true);
        }

        UpdateBurnSorting();
        if (!_burnVfxInstance.activeSelf)
            _burnVfxInstance.SetActive(true);
    }

    /// 화상 종료 또는 몬스터 비활성화 시 애니메이션 이펙트를 숨긴다.
    private void StopBurnVisual()
    {
        if (_burnVfxInstance != null)
            _burnVfxInstance.SetActive(false);
    }

    private void LateUpdate()
    {
        UpdateBurnSorting();
        UpdateSlowSorting();
        UpdateStaggerSorting();
        UpdateKnockbackSorting();
    }

    /// 바람 단일 원소 적중 시 탄환의 월드 진행 방향에 맞춰 고리 연출을 회전하고 다시 재생한다.
    private void PlayKnockbackVisual(Vector2 direction)
    {
        if (_knockbackVfxPrefab == null)
            return;

        if (_knockbackVfxInstance == null)
        {
            _knockbackVfxInstance = Instantiate(_knockbackVfxPrefab, transform, false);
            Vector3 center = _knockbackVfxTarget != null
                ? transform.InverseTransformPoint(_knockbackVfxTarget.bounds.center)
                : Vector3.zero;
            _knockbackVfxInstance.transform.localPosition = center + _knockbackVfxOffset;
            _knockbackVfxInstance.transform.localScale *= _knockbackVfxScale;
            _knockbackVfxRenderers = _knockbackVfxInstance.GetComponentsInChildren<SpriteRenderer>(true);
            _knockbackVfxAnimator = _knockbackVfxInstance.GetComponent<Animator>();
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + _knockbackVfxRotationOffset;
        _knockbackVfxInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        UpdateKnockbackSorting();
        if (!_knockbackVfxInstance.activeSelf)
            _knockbackVfxInstance.SetActive(true);
        if (_knockbackVfxAnimator != null)
            _knockbackVfxAnimator.Play(0, 0, 0f);
    }

    /// 넉백 종료 또는 몬스터 비활성화 시 넉백 애니메이션을 숨긴다.
    private void StopKnockbackVisual()
    {
        if (_knockbackVfxInstance != null)
            _knockbackVfxInstance.SetActive(false);
    }

    /// 몬스터의 Y 정렬에 맞춰 넉백 이펙트를 기존 디버프 연출 앞에 표시한다.
    private void UpdateKnockbackSorting()
    {
        if (_knockbackVfxTarget == null || _knockbackVfxRenderers == null)
            return;

        foreach (SpriteRenderer renderer in _knockbackVfxRenderers)
        {
            renderer.sortingLayerID = _knockbackVfxTarget.sortingLayerID;
            renderer.sortingOrder = _knockbackVfxTarget.sortingOrder + 4;
        }
    }

    /// 땅 단일 원소 경직 시 이펙트를 재사용하며 매 적중마다 애니메이션을 처음부터 재생한다.
    private void PlayStaggerVisual()
    {
        if (_staggerVfxPrefab == null)
            return;

        if (_staggerVfxInstance == null)
        {
            _staggerVfxInstance = Instantiate(_staggerVfxPrefab, transform, false);
            Vector3 center = _staggerVfxTarget != null
                ? transform.InverseTransformPoint(_staggerVfxTarget.bounds.center)
                : Vector3.zero;
            _staggerVfxInstance.transform.localPosition = center + _staggerVfxOffset;
            _staggerVfxInstance.transform.localScale *= _staggerVfxScale;
            _staggerVfxRenderers = _staggerVfxInstance.GetComponentsInChildren<SpriteRenderer>(true);
            _staggerVfxAnimator = _staggerVfxInstance.GetComponent<Animator>();
        }

        UpdateStaggerSorting();
        if (!_staggerVfxInstance.activeSelf)
            _staggerVfxInstance.SetActive(true);
        _isStaggerVfxRetracting = false;
        if (_staggerVfxAnimator != null)
        {
            _staggerVfxAnimator.speed = 1f;
            _staggerVfxAnimator.Play(0, 0, 0f);
        }
    }

    /// 석화 중에는 가시가 모두 올라온 프레임에서 경직 연출을 멈춰 둔다.
    private void HoldStaggerVisualWhilePetrified()
    {
        if (!IsStaggerVfxPlaying() || _staggerVfxAnimator.speed == 0f)
            return;

        if (_staggerVfxAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < StaggerVfxHoldTime)
            return;

        _staggerVfxAnimator.Play(0, 0, StaggerVfxHoldTime);
        _staggerVfxAnimator.speed = 0f;
    }

    /// 석화가 풀리면 멈춰 둔 가시가 내려가는 구간을 재생하고, 끝나면 숨긴다.
    private void RetractStaggerVisual()
    {
        if (!IsStaggerVfxPlaying())
            return;

        _staggerVfxAnimator.speed = 1f;
        float normalizedTime = _staggerVfxAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        _staggerVfxAnimator.Play(0, 0, Mathf.Max(normalizedTime, StaggerVfxRetractTime));
        _isStaggerVfxRetracting = true;
    }

    private bool IsStaggerVfxPlaying()
    {
        return _staggerVfxAnimator != null && _staggerVfxAnimator.isActiveAndEnabled;
    }

    private bool IsStaggerVfxFinished()
    {
        return !IsStaggerVfxPlaying() || _staggerVfxAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
    }

    /// 경직 종료 또는 몬스터 비활성화 시 경직 애니메이션을 숨긴다.
    private void StopStaggerVisual()
    {
        _isStaggerVfxRetracting = false;
        if (_staggerVfxAnimator != null)
            _staggerVfxAnimator.speed = 1f;
        if (_staggerVfxInstance != null)
            _staggerVfxInstance.SetActive(false);
    }

    /// 몬스터의 Y 정렬에 맞춰 경직 이펙트를 기존 디버프 연출 앞에 표시한다.
    private void UpdateStaggerSorting()
    {
        if (_staggerVfxTarget == null || _staggerVfxRenderers == null)
            return;

        foreach (SpriteRenderer renderer in _staggerVfxRenderers)
        {
            renderer.sortingLayerID = _staggerVfxTarget.sortingLayerID;
            renderer.sortingOrder = _staggerVfxTarget.sortingOrder + 3;
        }
    }

    /// 물 단일 원소 감속 시 하나의 이펙트를 생성하며 재적용은 기존 애니메이션을 유지한다.
    private void PlaySlowVisual()
    {
        if (_slowVfxPrefab == null)
            return;

        if (_slowVfxInstance == null)
        {
            _slowVfxInstance = Instantiate(_slowVfxPrefab, transform, false);
            Vector3 center = _slowVfxTarget != null
                ? transform.InverseTransformPoint(_slowVfxTarget.bounds.center)
                : Vector3.zero;
            _slowVfxInstance.transform.localPosition = center + _slowVfxOffset;
            _slowVfxInstance.transform.localScale *= _slowVfxScale;
            _slowVfxRenderers = _slowVfxInstance.GetComponentsInChildren<SpriteRenderer>(true);
        }

        UpdateSlowSorting();
        if (!_slowVfxInstance.activeSelf)
            _slowVfxInstance.SetActive(true);
    }

    /// 감속 종료 또는 몬스터 비활성화 시 감속 애니메이션을 숨긴다.
    private void StopSlowVisual()
    {
        if (_slowVfxInstance != null)
            _slowVfxInstance.SetActive(false);
    }

    /// 몬스터의 Y 정렬에 맞춰 감속 이펙트를 몸체와 화상 연출 앞에 표시한다.
    private void UpdateSlowSorting()
    {
        if (_slowVfxTarget == null || _slowVfxRenderers == null)
            return;

        foreach (SpriteRenderer renderer in _slowVfxRenderers)
        {
            renderer.sortingLayerID = _slowVfxTarget.sortingLayerID;
            renderer.sortingOrder = _slowVfxTarget.sortingOrder + 2;
        }
    }

    /// 몬스터의 Y 정렬에 맞춰 불꽃을 몸체 바로 앞에 표시한다.
    private void UpdateBurnSorting()
    {
        if (_burnVfxTarget == null || _burnVfxRenderers == null)
            return;

        foreach (SpriteRenderer renderer in _burnVfxRenderers)
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