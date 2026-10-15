using System;
using System.Collections.Generic;
using UnityEngine;

// 이동, 추적, 반전. 종료 여부는 Bullet에서 결정한다.
public sealed class BulletMovement
{
    private readonly Transform _transform;
    private readonly PlayerAttackData _attackData;
    private readonly BulletRuntime _runtime;
    private readonly float _ownerArrivalDistance;
    private readonly float _reverseSteeringDelay;
    private readonly float _orbitDuration;
    private readonly float _seekRadius;
    private readonly LayerMask _seekEnemyLayers;
    private readonly List<Collider2D> _seekCandidates = new(16);
    private readonly float _startSpeed, _endSpeed;
    private readonly float _seekTurnTime;
    private const int SeekCurveSteps = 32;
    private readonly Vector2[] _seekCurve = new Vector2[SeekCurveSteps + 1];
    private EnemyHealth _seekTarget;
    private Vector2 _seekDestination;
    private Vector2 _seekStart, _seekControl1, _seekControl2;
    private int _seekCurveIndex;
    private float _seekRepathRemaining;
    private float _deltaTime;
    public event Action<BulletEndReason> EndRequested;

    public BulletMovement(Transform transform, PlayerAttackData attackData, BulletRuntime runtime,
        float seekRadius, LayerMask seekLayers, float arrivalDistance, float reverseDelay, float orbitDuration,
        float startSpeed, float endSpeed, float seekTurnTime = 0.25f)
    {
        this._transform = transform;
        _attackData = attackData;
        _runtime = runtime;
        _seekRadius = seekRadius;
        _seekEnemyLayers = seekLayers;
        _ownerArrivalDistance = arrivalDistance;
        _reverseSteeringDelay = reverseDelay;
        _orbitDuration = orbitDuration;
        _startSpeed = startSpeed;
        _endSpeed = endSpeed;
        _seekTurnTime = Mathf.Max(0.01f, seekTurnTime);
    }

    public void Tick(float deltaTime)
    {
        _deltaTime = Mathf.Max(0f, deltaTime);
        switch (_runtime.Phase)
        {
            case BulletPhase.Flying:
                MoveFlying();
                break;
            case BulletPhase.Returning:
                MoveReturning();
                break;
            case BulletPhase.Orbiting:
                MoveOrbiting();
                break;
            case BulletPhase.Ended:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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
                UpdateSeekTarget(deltaTime);
            MoveFlyingInDirection(deltaTime);
        }

        if (_runtime.TravelDistance >= _attackData.Range)
            RequestEnd(BulletEndReason.MaxRange);
    }

    private void MoveFlyingInDirection(float deltaTime)
    {
        float remainingDistance = Mathf.Max(0f, _attackData.Range - _runtime.TravelDistance);
        float progress = _attackData.Range > 0f ? _runtime.TravelDistance / _attackData.Range : 1f;
        float remainingProgress = 1f - Mathf.Clamp01(progress);
        float easedProgress = 1f - remainingProgress * remainingProgress * remainingProgress * remainingProgress *
            remainingProgress;
        float speed = Mathf.Lerp(_startSpeed, _endSpeed, easedProgress) * _attackData.Configuration.SpeedMultiplier;
        float moveDistance = Mathf.Min(speed * deltaTime, remainingDistance);
        if (_seekTarget != null && _runtime.ReverseSteeringRemaining <= 0f)
            MoveAlongSeekCurve(moveDistance);
        else
            _transform.position += (Vector3)(_runtime.Direction * moveDistance);
        _runtime.TravelDistance += moveDistance;
        _runtime.TotalTravelDistance += moveDistance;
    }

    /// 일반 비행 중 가장 가까운 미적중 적을 추적하며, 없으면 현재 방향을 유지한다.
    private void UpdateSeekTarget(float deltaTime)
    {
        if (_seekRadius <= 0f)
            return;

        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_seekEnemyLayers);
        _seekCandidates.Clear();
        // 재사용 List는 필요 시 확장되어 고정 버퍼로 인한 대상 누락을 방지한다.
        Physics2D.OverlapCircle(_transform.position, _seekRadius, filter, _seekCandidates);

        EnemyHealth closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in _seekCandidates)
        {
            EnemyHealth enemy = candidate.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled || _runtime.HasHitTarget(enemy))
                continue;

            float distance = ((Vector2)(enemy.transform.position - _transform.position)).sqrMagnitude;
            if (distance >= closestDistance)
                continue;

            closest = enemy;
            closestDistance = distance;
        }

        _seekRepathRemaining -= deltaTime;
        if (closest == null)
        {
            _seekTarget = null;
            return;
        }

        Vector2 offset = closest.transform.position - _transform.position;
        if (offset.sqrMagnitude <= 0f)
            return;

        if (_seekTarget != closest || _seekCurveIndex > SeekCurveSteps ||
            (_seekRepathRemaining <= 0f &&
             ((Vector2)closest.transform.position - _seekDestination).sqrMagnitude > 0.01f))
        {
            _seekTarget = closest;
            BuildSeekCurve(closest.transform.position);
        }
    }

    private void BuildSeekCurve(Vector2 destination)
    {
        _seekStart = _transform.position;
        _seekDestination = destination;
        Vector2 forward = _runtime.Direction.normalized;
        Vector2 toTarget = (destination - _seekStart).normalized;
        float progress = _attackData.Range > 0f ? _runtime.TravelDistance / _attackData.Range : 1f;
        float speed = Mathf.Lerp(_startSpeed, _endSpeed, 1f - Mathf.Pow(1f - Mathf.Clamp01(progress), 5f)) *
                      _attackData.Configuration.SpeedMultiplier;
        float handle = Mathf.Min(Mathf.Max(0.01f, speed * _seekTurnTime),
            Vector2.Distance(_seekStart, destination) * 0.5f);
        _seekControl1 = _seekStart + forward * handle;
        _seekControl2 = destination - toTarget * Mathf.Min(handle, Vector2.Distance(_seekStart, destination) * 0.5f);
        if (Vector2.Dot(forward, toTarget) < -0.5f)
        {
            float side = forward.x * toTarget.y - forward.y * toTarget.x;
            Vector2 lateral = new Vector2(-forward.y, forward.x) * (side < 0f ? -1f : 1f);
            _seekControl2 += lateral * handle;
        }

        for (int i = 0; i <= SeekCurveSteps; i++)
        {
            float t = (float)i / SeekCurveSteps;
            float u = 1f - t;
            _seekCurve[i] = u * u * u * _seekStart + 3f * u * u * t * _seekControl1 +
                            3f * u * t * t * _seekControl2 + t * t * t * destination;
        }
        _seekCurveIndex = 1;
        _seekRepathRemaining = 0.1f;
    }

    private void MoveAlongSeekCurve(float distance)
    {
        Vector2 position = _transform.position;
        while (distance > 0f && _seekCurveIndex <= SeekCurveSteps)
        {
            Vector2 offset = _seekCurve[_seekCurveIndex] - position;
            float length = offset.magnitude;
            if (length <= 0.00001f)
            {
                _seekCurveIndex++;
                continue;
            }
            float step = Mathf.Min(distance, length);
            position += offset * (step / length);
            distance -= step;
            float segmentLength = Vector2.Distance(_seekCurve[_seekCurveIndex - 1], _seekCurve[_seekCurveIndex]);
            float fraction = segmentLength > 0f ? 1f - (length - step) / segmentLength : 1f;
            float t = (_seekCurveIndex - 1 + fraction) / SeekCurveSteps;
            float u = 1f - t;
            Vector2 tangent = u * u * (_seekControl1 - _seekStart) +
                              2f * u * t * (_seekControl2 - _seekControl1) +
                              t * t * (_seekDestination - _seekControl2);
            if (tangent.sqrMagnitude > 0.000001f)
                _runtime.Direction = tangent.normalized;
            if (step >= length)
                _seekCurveIndex++;
        }
        position += _runtime.Direction * distance;
        _transform.position = new Vector3(position.x, position.y, _transform.position.z);
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

        Vector2 offset = owner.position - _transform.position;
        if (offset.sqrMagnitude <= _ownerArrivalDistance * _ownerArrivalDistance)
        {
            RequestEnd(BulletEndReason.OwnerReached);
            return;
        }

        float deltaTime = _deltaTime;
        float reverseTime = Mathf.Min(deltaTime, _runtime.ReverseSteeringRemaining);
        if (reverseTime > 0f)
        {
            _transform.position += (Vector3)(_runtime.Direction * (_attackData.Speed * reverseTime));
            _runtime.TotalTravelDistance += _attackData.Speed * reverseTime;
            _runtime.ReverseSteeringRemaining -= reverseTime;
            deltaTime -= reverseTime;
        }

        if (deltaTime > 0f)
        {
            offset = owner.position - _transform.position;
            _runtime.Direction = offset.normalized;
            float distance = Mathf.Min(_attackData.Speed * deltaTime, offset.magnitude);
            _transform.position += (Vector3)(_runtime.Direction * distance);
            _runtime.TotalTravelDistance += distance;
            UpdateRotation();
        }

        if (((Vector2)(owner.position - _transform.position)).sqrMagnitude <=
            _ownerArrivalDistance * _ownerArrivalDistance)
            RequestEnd(BulletEndReason.OwnerReached);
    }

    public void EnterOrbiting()
    {
        Vector2 offset = _transform.position - _attackData.Owner.position;
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

        float deltaTime = Mathf.Min(_deltaTime, Mathf.Max(0f, _orbitDuration - _runtime.OrbitElapsed));
        float distance = Mathf.Max(0f, _attackData.Speed) * deltaTime;
        _runtime.OrbitAngle += distance / _runtime.OrbitRadius * _runtime.OrbitDirection;
        Vector2 radial = new Vector2(Mathf.Cos(_runtime.OrbitAngle), Mathf.Sin(_runtime.OrbitAngle));
        Vector2 nextPosition = (Vector2)_attackData.Owner.position + radial * _runtime.OrbitRadius;
        _transform.position = new Vector3(nextPosition.x, nextPosition.y, _transform.position.z);
        _runtime.TotalTravelDistance += distance;
        _runtime.OrbitElapsed += deltaTime;
        UpdateOrbitDirection();

        if (_runtime.OrbitElapsed >= _orbitDuration)
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
        _seekTarget = null;
        _runtime.ReverseSteeringRemaining = _reverseSteeringDelay;
        UpdateRotation();
        return true;
    }

    public void UpdateRotation()
    {
        float angle = Mathf.Atan2(_runtime.Direction.y, _runtime.Direction.x) * Mathf.Rad2Deg;
        _transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
