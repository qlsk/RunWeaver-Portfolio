using System.Collections.Generic;

public class RuneEffectContext
{
    private RuneEffectStage _stage; // 룬 효과를 적용하는 단계
    private int _slotIndex; // 룬 효과 슬롯의 인덱스
    private PlayerCombatStats _baseStats; // 플레이어의 기본 전투 스탯
    private RuneAttackSnapshot _attack;
    private RuneProjectileContext _projectile;
    private EnemyHealth _hitTarget;
    private float _deltaTime;
    private bool _shouldEnd;

    private List<CombatStatModifier> _statModifiers;
    private List<RuneProjectileContext> _projectiles;

    public RuneEffectStage Stage => _stage;
    public int SlotIndex => _slotIndex;
    public PlayerCombatStats BaseStats => _baseStats;
    public RuneAttackSnapshot Attack => _attack;
    public RuneProjectileContext Projectile => _projectile;
    public EnemyHealth HitTarget => _hitTarget;
    public float DeltaTime => _deltaTime;
    public bool ShouldEnd => _shouldEnd;

    public IReadOnlyList<CombatStatModifier> StatModifiers => _statModifiers;
    public IReadOnlyList<RuneProjectileContext> AddedProjectiles => _projectiles;

    public RuneEffectContext(RuneEffectStage stage, int slotIndex, PlayerCombatStats baseStats,
        RuneAttackSnapshot attack, RuneProjectileContext projectile, EnemyHealth hitTarget, float deltaTime)
    {
        _stage = stage;
        _slotIndex = slotIndex;
        _baseStats = baseStats;
        _attack = attack;
        _projectile = projectile;
        _hitTarget = hitTarget;
        _deltaTime = deltaTime;

        _statModifiers = new List<CombatStatModifier>();
        _projectiles = new List<RuneProjectileContext>();
    }

    public void AddStatModifier(CombatStatModifier modifier)
    {
        _statModifiers.Add(modifier);
    }

    public void AddProjectile(RuneProjectileContext projectile)
    {
        _projectiles.Add(projectile);
    }
}