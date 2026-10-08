/// <summary>
/// 특성 효과 종류. 카테고리 핸들러 안에서 2차 분기 기준으로 쓴다.
/// 새 특성은 SO 1개 + 핸들러에 case 1줄이면 된다.
/// </summary>
public enum TraitEffectKind
{
    // Attack
    DamageMult,
    FireRate,
    ReactionDamage,
    EchoBullet,
    Mark,
    // Dash
    DashDistance,
    DashCooldown,
    DamageReductionWindow,
    DashPathDamage,
    NextAttackBuff,
    // Shield
    MaxShieldStack,
    InvincibleDuration,
    RechargeTime,
    ShieldNova,
    ShieldedMoveSpeed,
    // Cleanse
    Immunity,
    MoveSpeedWindow,
    HealOnCleanse,
    CooldownRefund,
    CleanseNova
}
