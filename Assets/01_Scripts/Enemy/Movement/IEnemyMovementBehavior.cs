/// 근접·원거리 이동 행동이 목적지와 정지 조건을 제공하기 위한 계약이다.
public interface IEnemyMovementBehavior
{
    public float MoveSpeed { get; }
    MovementGoal CalculateGoal();
}
