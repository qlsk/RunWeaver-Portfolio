using System;
using UnityEngine;

/// 원소 하나의 발사, 비행, 적중 프리팹과 표시 설정.
[Serializable]
public sealed class BulletVisualEffects
{
    public ParticleSystem CastPrefab;
    public GameObject ProjectilePrefab;
    public ParticleSystem HitPrefab;
    [Min(0.01f)] public float CastScale = 1f;
    [Min(0.1f)] public float CastLifetime = 3f;
    [Min(0.01f)] public float ProjectileScale = 0.2f;
    /// 비행 VFX의 로컬 Y축 회전 각도(도).
    public float ProjectileRotationY = 90f;
    [Min(0.01f)] public float HitScale = 1f;
    [Min(0.1f)] public float HitLifetime = 3f;
}

public readonly struct PlayerAttackData
{
    public Vector2 Direction { get; }
    public Transform Owner { get; }
    public float Damage { get; }
    public float Speed { get; }
    public float Range { get; }
    public RuneEffectConfiguration Configuration { get; }

    public PlayerAttackData(Vector2 direction, float damage, float speed, float range,
        RuneEffectConfiguration configuration, Transform owner = null)
    {
        Direction = direction;
        Damage = damage;
        Speed = speed;
        Range = range;
        Configuration = configuration;
        Owner = owner;
    }

    public PlayerAttackData WithDirection(Vector2 direction)
    {
        return new PlayerAttackData(direction, Damage, Speed, Range, Configuration, Owner);
    }
}
