using System.Collections.Generic;
using UnityEngine;

public class RuneEffectPipeline
{
    private readonly RuneLoadout _loadout;
    private readonly RuneEffectDatabase _database;
    private readonly RuneEffectFactory _factory;

    public RuneEffectPipeline(RuneLoadout loadout, RuneEffectDatabase database, RuneEffectFactory factory)
    {
        _loadout = loadout;
        _database = database;
        _factory = factory;
    }

    public RuneAttackSnapshot CreateAttack(BulletStats baseStats) { }

    public IReadOnlyList<RuneProjectileContext> CreateProjectiles(RuneAttackSnapshot attack, Vector2 direction) { }
}