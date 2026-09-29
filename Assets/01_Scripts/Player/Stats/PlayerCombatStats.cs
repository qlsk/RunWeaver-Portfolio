using System;

public class PlayerCombatStats
{
    private readonly float _maxHealth;
    private readonly float _moveSpeed;
    private readonly float _fireInterval;
    private readonly float _bulletDamage;

    public float MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
    public float FireInterval => _fireInterval;
    public float BulletDamage => _bulletDamage;

    public PlayerCombatStats(float maxHealth, float moveSpeed, float fireInterval, float bulletDamage)
    {
        ValueValidator.EnsureFinite(maxHealth, nameof(maxHealth));
        ValueValidator.EnsureFinite(moveSpeed, nameof(moveSpeed));
        ValueValidator.EnsureFinite(fireInterval, nameof(fireInterval));
        ValueValidator.EnsureFinite(bulletDamage, nameof(bulletDamage));

        _maxHealth = maxHealth;
        _moveSpeed = moveSpeed;
        _fireInterval = fireInterval;
        _bulletDamage = bulletDamage;
    }

    public float GetValue(CombatStatType type)
    {
        return type switch
        {
            CombatStatType.MaxHealth => _maxHealth,
            CombatStatType.MoveSpeed => _moveSpeed,
            CombatStatType.FireInterval => _fireInterval,
            CombatStatType.BulletDamage => _bulletDamage,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
