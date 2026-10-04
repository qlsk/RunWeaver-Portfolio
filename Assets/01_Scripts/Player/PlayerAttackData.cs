using UnityEngine;

public readonly struct PlayerAttackData
{
    public Vector2 Direction { get; }
    public float Damage { get; }
    public float Speed { get; }
    public float Range { get; }
    public RuneEffectConfiguration Configuration { get; }

    public PlayerAttackData(Vector2 direction, float damage, float speed, float range,
        RuneEffectConfiguration configuration)
    {
        Direction = direction;
        Damage = damage;
        Speed = speed;
        Range = range;
        Configuration = configuration;
    }

    public PlayerAttackData WithDirection(Vector2 direction)
    {
        return new PlayerAttackData(direction, Damage, Speed, Range, Configuration);
    }
}
