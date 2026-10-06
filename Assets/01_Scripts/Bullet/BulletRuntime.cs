using System.Collections.Generic;
using UnityEngine;

// 탄환마다 독립적으로 생성하는 실행 상태. 상태 전환 시 전체를 초기화하지 않는다.
public class BulletRuntime
{
    private readonly HashSet<EnemyHealth> _hitTargets = new();

    public bool TryRegisterTarget(EnemyHealth target)
    {
        return target != null && _hitTargets.Add(target);
    }

    // 첫 적중은 100%, 7번째부터 70%로 유지한다.
    public float GetDamageMultiplier(bool isPierce)
    {
        return isPierce ? 1f - 0.05f * Mathf.Min(HitCount, 6) : 1f;
    }

    public void RecordHit()
    {
        if (HitCount < int.MaxValue)
            HitCount++;
    }

    // Bullet의 현재 이동 상태
    public BulletPhase Phase;
    // 발사 이후 반전을 반영하는 현재 진행 방향.
    public Vector2 Direction;
    // 완료한 직접 적중 횟수. 다음 적중의 Pierce 피해 감소에 사용한다.
    public int HitCount;
    // Reverse 사용 여부. Return/Orbit 진입 후에도 유지한다.
    public bool ReverseUsed;
    // Return 진입 여부. 한 탄환에서 두 번 복귀하지 않도록 한다.
    public bool ReturnUsed;

    // 복귀 중 Reverse 직후 반전 방향을 유지하는 남은 시간.
    public float ReverseSteeringRemaining;
    
    // Flying 중 누적 이동거리. 방향을 반전해도 감소하지 않는다.
    public float TravelDistance;
    // 성장 진행도(0~1). Flying/Returning/Orbiting 이동 모두 누적한다.
    public float GrowProgress;
    // Orbit 진입 이후 경과 시간(초).
    public float OrbitElapsed;
    public float OrbitRadius; // Orbit 진입 시 플레이어와의 거리로 정한 반경.
    public float OrbitAngle; // 플레이어 중심의 현재 회전 각도(라디안).
    public float OrbitDirection = 1f; // XY 평면에서 +1은 반시계, -1은 시계 방향.
}

// Fix는 별도 Trap을 생성하므로 탄환 이동 상태에는 포함하지 않는다.
public enum BulletPhase
{
    Flying,
    Returning,
    Orbiting,
    Ended
}
