using System.Collections.Generic;
using UnityEngine;

/// 일반 이동과 공격 요청을 모아 물리 업데이트 마지막에 적용한다.
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMotor : MonoBehaviour
{
    private Rigidbody2D _body;
    private EnemyHealth _health;
    private EnemyStatusEffectController _statusEffects;
    private readonly HashSet<MonoBehaviour> _positionLocks = new();
    private RigidbodyConstraints2D _savedConstraints;
    private bool _positionFrozen;
    private Vector2 _desiredVelocity;
    private bool _stopOnStagger;
    private MonoBehaviour _overrideOwner;
    private Vector2 _overrideVelocity;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _health = GetComponent<EnemyHealth>();
        _statusEffects = GetComponent<EnemyStatusEffectController>();
    }

    private void OnEnable()
    {
        EnemyHealth.OnDeath += HandleDeath;
    }

    /// 감속 전 희망 속도를 받는다. 요청은 다음 물리 적용에서 소비한다.
    public void ApplyMovement(Vector2 desiredVelocity, bool stopOnStagger = true)
    {
        _desiredVelocity = desiredVelocity;
        _stopOnStagger = stopOnStagger;
    }

    public void AcquirePositionLock(MonoBehaviour owner)
    {
        if (owner == null) return;
        _positionLocks.Add(owner);
        UpdatePositionLock();
        _body.linearVelocity = Vector2.zero;
    }

    public void ReleasePositionLock(MonoBehaviour owner)
    {
        _positionLocks.Remove(owner);
        UpdatePositionLock();
    }

    /// 돌진이나 탱크 공격에서 속도 덮어 씌우는 함수
    public bool SetOverrideVelocity(MonoBehaviour owner, Vector2 velocity)
    {
        if (owner == null)
        {
            return false;
        }

        if (_overrideOwner != null && _overrideOwner != owner)
        {
            return false;
        }

        _overrideOwner = owner;
        _overrideVelocity = velocity;
        return true;
    }

    /// 덮어 씌운 속도를 다시 초기화 하는 함수
    public void ClearOverride(MonoBehaviour owner)
    {
        if (_overrideOwner != owner) return;
        _overrideOwner = null;
        _overrideVelocity = Vector2.zero;
        if (_body != null) _body.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        _positionLocks.RemoveWhere(owner => owner == null || !owner.isActiveAndEnabled);
        UpdatePositionLock();
        Vector2 velocity = _desiredVelocity;
        _desiredVelocity = Vector2.zero;

        if (_health != null && _health.IsDead)
        {
            ClearAllRequests();
            return;
        }

        if (_positionFrozen)
        {
            velocity = Vector2.zero;
        }
        else if (_overrideOwner != null)
        {
            velocity = _overrideVelocity;
        }
        else
        {
            _overrideOwner = null;
            if (_statusEffects != null)
            {
                if (_statusEffects.IsRooted || _statusEffects.IsPulled ||
                    (_stopOnStagger && _statusEffects.IsStaggered))
                {
                    velocity = Vector2.zero;
                }
                else
                {
                    velocity *= _statusEffects.MoveSpeedMultiplier;
                }

                velocity += _statusEffects.KnockbackVelocity + _statusEffects.PullVelocity +
                            _statusEffects.PushVelocity;
            }
        }

        _body.linearVelocity = velocity;
    }

    private void UpdatePositionLock()
    {
        if (_body == null) return;
        if (_positionLocks.Count > 0 && !_positionFrozen)
        {
            _savedConstraints = _body.constraints;
            _body.constraints |= RigidbodyConstraints2D.FreezePosition;
            _positionFrozen = true;
        }
        else if (_positionLocks.Count == 0 && _positionFrozen)
        {
            _body.constraints = _savedConstraints;
            _positionFrozen = false;
        }
    }

    public void ClearAllRequests()
    {
        _desiredVelocity = Vector2.zero;
        _overrideOwner = null;
        _overrideVelocity = Vector2.zero;
        _positionLocks.Clear();
        UpdatePositionLock();
        if (_body != null) _body.linearVelocity = Vector2.zero;
    }

    private void HandleDeath(EnemyHealth health)
    {
        if (health == _health)
        {
            ClearAllRequests();
        }
    }

    private void OnDisable()
    {
        EnemyHealth.OnDeath -= HandleDeath;
        ClearAllRequests();
    }
}