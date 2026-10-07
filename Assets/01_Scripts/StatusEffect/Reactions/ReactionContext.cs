using UnityEngine;

/// 반응 실행 시점의 적중 위치, 탄환 피해와 진행 방향을 전달한다.
public readonly struct ReactionContext
{
    public Vector2 HitPosition { get; }
    public float BulletDamage { get; }
    public Vector2 BulletDirection { get; }

    public ReactionContext(Vector2 hitPosition, float bulletDamage, Vector2 bulletDirection)
    {
        HitPosition = hitPosition;
        BulletDamage = bulletDamage;
        BulletDirection = bulletDirection;
    }
}
