// 발사 전 Power/Speed/Range 등의 보정을 반영할 기본 능력치.
// Grow/Pierce 배율은 여기에 누적하지 않고 적중 순간 별도로 계산한다.
public class BulletStats
{
    // 마탄 데미지
    public float Damage;
    // 마탄 속도
    public float Speed;
    // 사거리
    public float Range;
    // 원소 효과 지속 시간. 현재 Bullet에서는 보관만 하며 Orbit/Trap 수명에는 사용하지 않는다.
    public float Duration;
}