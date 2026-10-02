using System;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("총구 발사 이펙트")]
    [SerializeField] private ParticleSystem _castEffectPrefab;
    [SerializeField, Min(0.01f)] private float _castEffectScale = 1f;
    [SerializeField, Min(0.1f)] private float _castEffectLifetime = 3f;

    [Header("룬 시스템 연결")]
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private LayerMask _enemyLayers = ~0;

    private BulletStats _stats;
    private BulletEffectConfig _effects;
    private BulletRuntime _runtime;
    private Transform _owner;
    private Vector2 _direction;
    private bool _initialized;
    private bool _processingHit;
    private float _reverseSteeringRemaining;
    private readonly Dictionary<Collider2D, EnemyHealth> _contacts = new();
    private readonly HashSet<EnemyHealth> _contactTargets = new();
    private readonly List<Collider2D> _staleContacts = new();

    public event Action<EnemyHealth, float> HitEffectsRequested;
    public event Action<EnemyHealth> ElementReactionRequested;
    public event Action<EnemyHealth> ChainRequested;
    public Transform Owner => _owner;
    public BulletPhase Phase => _runtime == null ? BulletPhase.Ended : _runtime.Phase;

    public void PlayCastEffect(Transform muzzle)
    {
        if (_castEffectPrefab == null || muzzle == null) return;

        ParticleSystem effect = Instantiate(_castEffectPrefab, muzzle, false);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        effect.transform.localScale *= _castEffectScale;

        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.stopAction = ParticleSystemStopAction.None;
            var trails = particles.trails;
            trails.worldSpace = false;
        }

        effect.gameObject.SetActive(true);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
        Destroy(effect.gameObject, _castEffectLifetime);
    }

    // 능력치, 룬 설정, 소유자 방향을 받아서 발사 준비, 탄환별 상태와 충돌 기록, 이전 이벤트 연결 초기화
    public void Initialize(BulletStats stats, BulletEffectConfig effects, Transform owner, Vector2 direction)
    {
        if (stats == null) throw new ArgumentNullException(nameof(stats));
        if (effects == null) throw new ArgumentNullException(nameof(effects));
        _stats = new BulletStats
        {
            Damage = Mathf.Max(0f, stats.Damage), Speed = Mathf.Max(0f, stats.Speed),
            Range = Mathf.Max(0f, stats.Range), Duration = Mathf.Max(0f, stats.Duration)
        };
        _effects = effects;
        _owner = owner;
        _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        _runtime = new BulletRuntime { Phase = BulletPhase.Flying };
        _contacts.Clear();
        _contactTargets.Clear();
        _reverseSteeringRemaining = 0f;
        _processingHit = false;
        HitEffectsRequested = null;
        ElementReactionRequested = null;
        ChainRequested = null;
        _initialized = true;
        UpdateRotation();
    }

    // 접촉 기록 정리 -> 이동 -> Grow 갱신 -> 종료 조건 확인
    private void Update()
    {
        if (!_initialized || Phase == BulletPhase.Ended) return;
        RemoveStaleContacts();
        float movedDistance = UpdateMovement();
        UpdateGrow(movedDistance);
        _reverseSteeringRemaining = Mathf.Max(0f, _reverseSteeringRemaining - Time.deltaTime);
        CheckEndCondition();
    }

    // 현재 상태에 맞는 이동 함수 호출, Grow에 사용할 이동 거리 반환
    private float UpdateMovement()
    {
        switch (Phase)
        {
            case BulletPhase.Flying:
                if (_effects.Seek && _reverseSteeringRemaining <= 0f) UpdateSeekDirection();
                return MoveForward();
            case BulletPhase.Returning: return MoveToOwner();
            case BulletPhase.Orbiting: return MoveAroundOwner();
            default: return 0f;
        }
    }

    // 범위 내 가장 가까운 살아 있는 적을 찾아 _direction 변경(위치 변경 X)
    private void UpdateSeekDirection()
    {
        EnemyHealth closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in Physics2D.OverlapCircleAll(transform.position, _effects.SeekRadius,
                     _enemyLayers))
        {
            EnemyHealth enemy = candidate.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            float distance = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
            if (distance >= closestDistance) continue;
            closest = enemy;
            closestDistance = distance;
        }

        if (closest == null) return;
        Vector2 direction = closest.transform.position - transform.position;
        if (direction.sqrMagnitude > 0f) _direction = direction.normalized;
    }

    // 현재 방향으로 이동하고 일반 비행의 누적 이동거리 증가
    private float MoveForward()
    {
        float remaining = Mathf.Max(0f, _stats.Range - _runtime.TravelDistance);
        float distance = Mathf.Min(_stats.Speed * Time.deltaTime, remaining);
        MoveInDirection(distance);
        _runtime.TravelDistance += distance;
        return distance;
    }

    // 플레이어의 현재 위치로 복귀, Reverse 직후에는 잠시 반전 방향 유지
    private float MoveToOwner()
    {
        if (_owner == null)
        {
            EndImmediately();
            return 0f;
        }

        float distance = _stats.Speed * Time.deltaTime;
        if (_reverseSteeringRemaining <= 0f)
        {
            Vector2 offset = _owner.position - transform.position;
            distance = Mathf.Min(distance, offset.magnitude);
            if (offset.sqrMagnitude > 0f) _direction = offset.normalized;
        }

        MoveInDirection(distance);
        return distance;
    }

    // _directio 방향으로 전달받은 거리만큼 실제 위치 이동
    private void MoveInDirection(float distance)
    {
        transform.position += (Vector3)(_direction * distance);
        UpdateRotation();
    }

    // 탄환 외형이 진행 방향을 바라보도록 회전
    private void UpdateRotation()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
    }

    // Orbit 상태로 전환하고 회전 반경, 시작 각도, 경과 시간 설정
    private void EnterOrbiting()
    {
        if (_owner == null)
        {
            EndImmediately();
            return;
        }

        Vector2 offset = transform.position - _owner.position;
        _runtime.Phase = BulletPhase.Orbiting;
        _runtime.OrbitElapsed = 0f;
        _runtime.OrbitRadius = Mathf.Max(offset.magnitude, 0.01f);
        _runtime.OrbitAngle = Mathf.Atan2(offset.y, offset.x);
    }

    // 플레이어 주변을 회전하고 Orbit 경과 시간 증가
    private float MoveAroundOwner()
    {
        if (_owner == null)
        {
            EndImmediately();
            return 0f;
        }

        float deltaTime = Mathf.Min(Time.deltaTime, Mathf.Max(0f, _effects.OrbitDuration - _runtime.OrbitElapsed));
        float distance = _stats.Speed * deltaTime;
        _runtime.OrbitAngle += distance / _runtime.OrbitRadius * _runtime.OrbitDirection;
        Vector2 radial = new Vector2(Mathf.Cos(_runtime.OrbitAngle), Mathf.Sin(_runtime.OrbitAngle));
        Vector2 next = (Vector2)_owner.position + radial * _runtime.OrbitRadius;
        transform.position += (Vector3)(next - (Vector2)transform.position);
        _direction = new Vector2(-radial.y, radial.x) * _runtime.OrbitDirection;
        UpdateRotation();
        _runtime.OrbitElapsed += deltaTime;
        return distance;
    }

    // 이동 거리에 따라 성장 진행도를 0~1로 누적, 실제 피해 배율은 적중할 때 계산
    private void UpdateGrow(float moveDistance)
    {
        if (Phase == BulletPhase.Ended || !_effects.Grow || _effects.GrowDistanceToMax <= 0f) return;
        _runtime.GrowProgress = Mathf.Clamp01(_runtime.GrowProgress + moveDistance / _effects.GrowDistanceToMax);
    }

    // 충돌 대상이 유효한 적인지 확인하고, 이번 접촉에서 처음 맞힌 적이면 ProcessHit() 호출
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_initialized || Phase == BulletPhase.Ended) return;
        EnemyHealth target = collision.GetComponentInParent<EnemyHealth>();
        if (target == null || target.IsDead || !target.isActiveAndEnabled) return;
        _contacts[collision] = target;
        if (_contactTargets.Add(target))
        {
            ProcessHit(target);
        }
    }

    // 접촉 기록 해제, 해당 적의 모든 콜라이더를 벗어나면 다음 진입에서 재적중 허용
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!_contacts.TryGetValue(collision, out EnemyHealth target)) return;
        _contacts.Remove(collision);
        if (!_contacts.ContainsValue(target))
        {
            _contactTargets.Remove(target);
        }
    }

    // 파괴되거나 비활성화되어 접촉 종료 콜백이 누락된 기록 정리
    private void RemoveStaleContacts()
    {
        _staleContacts.Clear();
        foreach (var contact in _contacts)
        {
            if (contact.Key == null || !contact.Key.isActiveAndEnabled || contact.Value == null)
            {
                _staleContacts.Add(contact.Key);
            }
        }

        foreach (Collider2D collider in _staleContacts)
        {
            EnemyHealth target = _contacts[collider];
            _contacts.Remove(collider);
            if (!_contacts.ContainsValue(target))
            {
                _contactTargets.Remove(target);
            }
        }
    }

    // 피해 적용, 적중 효과, 원소 반응, Chain, Reverse, 종료 판단의 실행 순서 관리
    public void ProcessHit(EnemyHealth target)
    {
        if (!_initialized || Phase == BulletPhase.Ended || _processingHit ||
            target == null || target.IsDead || !target.isActiveAndEnabled) return;
        _processingHit = true;
        try
        {
            float actualDamage = ApplyDamage(target, CalculateDamage());
            _runtime.HitCount++;
            ApplyHitEffects(target, actualDamage);
            ApplyElementReaction(target);
            ApplyChain(target);
            ApplyReverse();
            if (!_effects.Pierce)
            {
                RequestEnd(BulletEndReason.Hit);
            }
        }
        finally
        {
            _processingHit = false;
        }
    }

    // 기본 피해량에 현재 Grow, Pierce 배율을 적용해 이번 피해 계산
    private float CalculateDamage()
    {
        float grow = _effects.Grow ? Mathf.Lerp(1f, 2f, _runtime.GrowProgress) : 1f;
        float pierce = _effects.Pierce ? Mathf.Max(0.7f, 1f - 0.05f * _runtime.HitCount) : 1f;
        return _stats.Damage * grow * pierce;
    }

    // 적에게 피해를 적용하고 실제 체력 감소량 반환
    private float ApplyDamage(EnemyHealth enemy, float damage)
    {
        return enemy.ApplyDamage(damage);
    }

    // Bind-Absorb,Break 등을 처리할 외부 시스템에 이벤트 전달
    private void ApplyHitEffects(EnemyHealth enemy, float actualDamage)
    {
        HitEffectsRequested?.Invoke(enemy, actualDamage);
    }

    // 직접 적중으로 인한 원소 반응을 외부 시스템에 요청
    private void ApplyElementReaction(EnemyHealth enemy)
    {
        ElementReactionRequested?.Invoke(enemy);
    }

    // 직접 적중을 기준으로 Chain 처리를 외부 시스템에 요청
    private void ApplyChain(EnemyHealth enemy)
    {
        ChainRequested?.Invoke(enemy);
    }

    // Reverse 적용 여부와 사용 여부를 확인해 탄환당 한 번 반전
    private void ApplyReverse()
    {
        if (Phase == BulletPhase.Ended || !_effects.Reverse || _runtime.ReverseUsed) return;
        if (Phase == BulletPhase.Orbiting)
        {
            ReverseOrbitDirection();
        }
        else
        {
            _direction = -_direction;
            _reverseSteeringRemaining = _effects.ReverseSteeringDelay;
            UpdateRotation();
        }

        _runtime.ReverseUsed = true;
    }

    // Orbit의 회전 방향과 탄환 외형 방향 반전
    private void ReverseOrbitDirection()
    {
        _runtime.OrbitDirection *= -1f;
        _direction = -_direction;
        UpdateRotation();
    }

    public enum BulletEndReason
    {
        Hit,
        MaxRange,
        OrbitTimeout,
        OwnerReached
    }

    // 상태에 따라 사거리 도달, 플레이어 도착, Orbit 시간 만료 확인
    private void CheckEndCondition()
    {
        switch (Phase)
        {
            case BulletPhase.Flying:
                if (_runtime.TravelDistance >= _stats.Range)
                {
                    RequestEnd(BulletEndReason.MaxRange);
                }
                else if (_stats.Speed <= 0f)
                {
                    EndImmediately();
                }

                break;
            case BulletPhase.Returning:
                if (_owner == null || _stats.Speed <= 0f)
                {
                    EndImmediately();
                }
                else if (Vector2.Distance(transform.position, _owner.position) <= 0.1f)
                {
                    RequestEnd(BulletEndReason.OwnerReached);
                }

                break;
            case BulletPhase.Orbiting:
                if (_runtime.OrbitElapsed >= _effects.OrbitDuration)
                {
                    RequestEnd(BulletEndReason.OrbitTimeout);
                }

                break;
        }
    }

    // 종료 요청에 Return, Orbit, Fix가 개입할 수 있는지 판단
    public void RequestEnd(BulletEndReason reason)
    {
        if (!_initialized || Phase == BulletPhase.Ended) return;
        if (Phase == BulletPhase.Flying)
        {
            if (_effects.EndEffect == BulletEndEffect.Return && !_runtime.ReturnUsed)
            {
                if (_owner == null)
                {
                    EndImmediately();
                    return;
                }

                _runtime.ReturnUsed = true;
                _runtime.Phase = BulletPhase.Returning;
                return;
            }

            if (reason == BulletEndReason.MaxRange)
            {
                if (_effects.EndEffect == BulletEndEffect.Orbit)
                {
                    EnterOrbiting();
                    return;
                }

                if (_effects.EndEffect == BulletEndEffect.Fix)
                {
                    SpawnTrap();
                }
            }
        }

        EndImmediately();
    }

    // Fix용 독립 프리팹을 현재 위치에 생성하고 수명 지정
    private void SpawnTrap()
    {
        if (_trapPrefab == null)
        {
            Debug.LogWarning("Fix 룬에 사용할 Trap 프리팹이 지정되지 않았습니다.", this);
            return;
        }

        GameObject trap = Instantiate(_trapPrefab, transform.position, Quaternion.identity);
        Destroy(trap, _effects.TrapDuration);
    }

    // 상태를 Ended로 바꿔 추가 처리를 막고 탄환 파괴
    private void EndImmediately()
    {
        if (!_initialized || Phase == BulletPhase.Ended) return;
        _runtime.Phase = BulletPhase.Ended;
        _initialized = false;
        Destroy(gameObject);
    }
}
