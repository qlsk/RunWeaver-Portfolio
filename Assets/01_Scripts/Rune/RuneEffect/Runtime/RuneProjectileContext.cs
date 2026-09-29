using UnityEngine;

public class RuneProjectileContext
{
    private RuneAttackSnapshot _attack;
    private Vector2 _direction;
    private float _damageMultiplier;
    private float _spawnDelay;
    private bool _isEnded;

    public RuneAttackSnapshot Attack => _attack;
    public Vector2 Direction => _direction;
    public float DamageMultiplier => _damageMultiplier;
    public float SpawnDelay => _spawnDelay;
    public bool IsEnded => _isEnded;

    public RuneProjectileContext(RuneAttackSnapshot attack, Vector2 direction)
    {
        _attack = attack;
        _direction = direction;
    }

    public bool TryMarkEnded()
    {
        if (_isEnded)
        {
            return false;
        }
        _isEnded = true;
        return true;
    }
}