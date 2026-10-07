using System;
using System.Collections.Generic;
using UnityEngine;

// 이동, 추적, 반전. 종료 여부는 Bullet에서 결정한다.
public sealed class BulletMovement
{
    private readonly Transform transform;
    private readonly PlayerAttackData _attackData;
    private readonly BulletRuntime _runtime;
    private readonly float OwnerArrivalDistance, ReverseSteeringDelay, OrbitDuration, _seekRadius;
    private readonly LayerMask _seekEnemyLayers;
    private readonly List<Collider2D> _seekCandidates = new(16);
    private float _deltaTime;
    public event Action<BulletEndReason> EndRequested;

    public BulletMovement(Transform transform, PlayerAttackData attackData, BulletRuntime runtime,
        float seekRadius, LayerMask seekLayers, float arrivalDistance, float reverseDelay, float orbitDuration)
    {
        this.transform = transform;
        _attackData = attackData;
        _runtime = runtime;
        _seekRadius = seekRadius;
        _seekEnemyLayers = seekLayers;
        OwnerArrivalDistance = arrivalDistance;
        ReverseSteeringDelay = reverseDelay;
        OrbitDuration = orbitDuration;
    }
    public void Tick(float deltaTime)
    {
        _deltaTime = Mathf.Max(0f, deltaTime);
        if (_runtime.Phase == BulletPhase.Flying) MoveFlying();
        else if (_runtime.Phase == BulletPhase.Returning) MoveReturning();
        else if (_runtime.Phase == BulletPhase.Orbiting) MoveOrbiting();
    }
    private void RequestEnd(BulletEndReason reason) => EndRequested?.Invoke(reason);
    private void MoveFlying()
    {
        float deltaTime = _deltaTime;
        float reverseTime = Mathf.Min(deltaTime, _runtime.ReverseSteeringRemaining);
        if (reverseTime > 0f)
        {
            MoveFlyingInDirection(reverseTime);
            _runtime.ReverseSteeringRemaining -= reverseTime;
            deltaTime -= reverseTime;
        }

        if (deltaTime > 0f && _runtime.TravelDistance < _attackData.Range)
        {
            if (_attackData.Configuration.IsSeek)
                UpdateSeekDirection();
            MoveFlyingInDirection(deltaTime);
        }

        if (_runtime.TravelDistance >= _attackData.Range)
            RequestEnd(BulletEndReason.MaxRange);
    }

    private void MoveFlyingInDirection(float deltaTime)
    {
        float remainingDistance = Mathf.Max(0f, _attackData.Range - _runtime.TravelDistance);
        float moveDistance = Mathf.Min(_attackData.Speed * deltaTime, remainingDistance);
        transform.position += (Vector3)(_runtime.Direction * moveDistance);
        _runtime.TravelDistance += moveDistance;
        _runtime.TotalTravelDistance += moveDistance;
    }

    /// 일반 비행 중 가장 가까운 미적중 적을 추적하며, 없으면 현재 방향을 유지한다.
    private void UpdateSeekDirection()
    {
        if (_seekRadius <= 0f)
            return;

        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_seekEnemyLayers);
        _seekCandidates.Clear();
        // 재사용 List는 필요 시 확장되어 고정 버퍼로 인한 대상 누락을 방지한다.
        Physics2D.OverlapCircle(transform.position, _seekRadius, filter, _seekCandidates);

        EnemyHealth closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in _seekCandidates)
        {
            EnemyHealth enemy = candidate.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled || _runtime.HasHitTarget(enemy))
                continue;

            float distance = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closest = enemy;
            closestDistance = distance;
        }

        if (closest == null)
            return;

        Vector2 offset = closest.transform.position - transform.position;
        if (offset.sqrMagnitude <= 0f)
            return;

        _runtime.Direction = offset.normalized;
        UpdateRotation();
    }

    private void MoveReturning()
    {
        Transform owner = _attackData.Owner;
        if (owner == null)
        {
            RequestEnd(BulletEndReason.OwnerLost);
            return;
        }

        Vector2 offset = owner.position - transform.position;
        if (offset.sqrMagnitude <= OwnerArrivalDistance * OwnerArrivalDistance)
        {
            RequestEnd(BulletEndReason.OwnerReached);
            return;
        }

        float deltaTime = _deltaTime;
        float reverseTime = Mathf.Min(deltaTime, _runtime.ReverseSteeringRemaining);
        if (reverseTime > 0f)
        {
            transform.position += (Vector3)(_runtime.Direction * (_attackData.Speed * reverseTime));
            _runtime.TotalTravelDistance += _attackData.Speed * reverseTime;
            _runtime.ReverseSteeringRemaining -= reverseTime;
            deltaTime -= reverseTime;
        }

        if (deltaTime > 0f)
        {
            offset = owner.position - transform.position;
            _runtime.Direction = offset.normalized;
            float distance = Mathf.Min(_attackData.Speed * deltaTime, offset.magnitude);
            transform.position += (Vector3)(_runtime.Direction * distance);
            _runtime.TotalTravelDistance += distance;
            UpdateRotation();
        }

        if (((Vector2)(owner.position - transform.position)).sqrMagnitude <=
            OwnerArrivalDistance * OwnerArrivalDistance)
            RequestEnd(BulletEndReason.OwnerReached);
    }

    public void EnterOrbiting()
    {
        Vector2 offset = transform.position - _attackData.Owner.position;
        _runtime.Phase = BulletPhase.Orbiting;
        _runtime.OrbitElapsed = 0f;
        _runtime.OrbitRadius = Mathf.Max(offset.magnitude, 0.01f);
        _runtime.OrbitAngle = Mathf.Atan2(offset.y, offset.x);
        _runtime.ReverseSteeringRemaining = 0f;
        UpdateOrbitDirection();
    }

    /// 플레이어의 현재 위치를 중심으로 탄환 속도를 접선 속도로 사용해 5초간 회전한다.
    private void MoveOrbiting()
    {
        if (_attackData.Owner == null)
        {
            RequestEnd(BulletEndReason.OwnerLost);
            return;
        }

        float deltaTime = Mathf.Min(_deltaTime, Mathf.Max(0f, OrbitDuration - _runtime.OrbitElapsed));
        float distance = Mathf.Max(0f, _attackData.Speed) * deltaTime;
        _runtime.OrbitAngle += distance / _runtime.OrbitRadius * _runtime.OrbitDirection;
        Vector2 radial = new Vector2(Mathf.Cos(_runtime.OrbitAngle), Mathf.Sin(_runtime.OrbitAngle));
        Vector2 nextPosition = (Vector2)_attackData.Owner.position + radial * _runtime.OrbitRadius;
        transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
        _runtime.TotalTravelDistance += distance;
        _runtime.OrbitElapsed += deltaTime;
        UpdateOrbitDirection();

        if (_runtime.OrbitElapsed >= OrbitDuration)
            RequestEnd(BulletEndReason.OrbitTimeout);
    }

    private void UpdateOrbitDirection()
    {
        _runtime.Direction = new Vector2(-Mathf.Sin(_runtime.OrbitAngle), Mathf.Cos(_runtime.OrbitAngle)) *
                             _runtime.OrbitDirection;
        UpdateRotation();
    }

    /// 탄환의 전체 생명주기에서 한 번만 반전하고 이번 적중의 발동 여부를 반환한다.
    public bool ApplyReverse()
    {
        if (!_attackData.Configuration.IsReverse || _runtime.ReverseUsed)
            return false;

        _runtime.ReverseUsed = true;
        if (_runtime.Phase == BulletPhase.Orbiting)
        {
            _runtime.OrbitDirection *= -1f;
            UpdateOrbitDirection();
            return true;
        }

        _runtime.Direction = -_runtime.Direction;
        _runtime.ReverseSteeringRemaining = ReverseSteeringDelay;
        UpdateRotation();
        return true;
    }

    public void UpdateRotation()
    {
        float angle = Mathf.Atan2(_runtime.Direction.y, _runtime.Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

}
