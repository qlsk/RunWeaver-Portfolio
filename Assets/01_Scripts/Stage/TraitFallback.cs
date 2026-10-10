using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특성 20종 내장 기본값. _traitPool이 비어 있을 때 쓴다.
/// 수치·문구는 기획표 그대로. 에셋(TraitData)으로 교체하면 쓰이지 않는다.
/// </summary>
public static class TraitFallback
{
    public static List<TraitData> Build()
    {
        return new List<TraitData>
        {
            Make("attack_01", TraitCategory.Attack, TraitEffectKind.DamageMult,
                "응축된 마력", "공격 피해량이 20% 증가합니다.", 0.2f, 0f, 1, 0f),
            Make("attack_02", TraitCategory.Attack, TraitEffectKind.FireRate,
                "속사의 문양", "공격 속도가 15% 증가합니다.", 0.15f, 0f, 1, 0f),
            Make("attack_03", TraitCategory.Attack, TraitEffectKind.ReactionDamage,
                "원소력 강화", "원소 반응 피해량이 20% 증가합니다.", 0.2f, 0f, 1, 0f),
            Make("attack_04", TraitCategory.Attack, TraitEffectKind.EchoBullet,
                "메아리 탄환", "공격 5회마다 같은 방향으로 기본 공격 피해량의 50%를 주는 추가 탄환을 발사합니다. 추가 탄환은 공격 횟수에 포함되지 않습니다.", 0.5f, 0f, 5, 0f),
            Make("attack_05", TraitCategory.Attack, TraitEffectKind.Mark,
                "사냥의 낙인", "적중한 적에게 3초간 표식을 남깁니다. 표식이 있는 적에게 가하는 공격 피해량이 15% 증가합니다. 표식은 중첩되지 않습니다.", 0.15f, 3f, 1, 0f),
            Make("dash_01", TraitCategory.Dash, TraitEffectKind.DashDistance,
                "바람걸음", "대시 거리가 20% 증가합니다.", 0.2f, 0f, 1, 0f),
            Make("dash_02", TraitCategory.Dash, TraitEffectKind.DashCooldown,
                "되찾은 호흡", "대시 재사용 대기시간이 15% 감소합니다.", 0.15f, 0f, 1, 0f),
            Make("dash_03", TraitCategory.Dash, TraitEffectKind.DamageReductionWindow,
                "잔상의 장막", "대시 종료 후 1초간 받는 피해가 30% 감소합니다.", 0.3f, 1f, 1, 0f),
            Make("dash_04", TraitCategory.Dash, TraitEffectKind.DashPathDamage,
                "돌파의 각인", "대시 경로의 적에게 기본 공격 피해량의 60%를 줍니다. 같은 적은 대시 한 번당 한 번만 타격합니다.", 0.6f, 0f, 1, 1.5f),
            Make("dash_05", TraitCategory.Dash, TraitEffectKind.NextAttackBuff,
                "반격의 발걸음", "대시 종료 후 2초 내 첫 공격의 피해량이 40% 증가합니다.", 0.4f, 2f, 1, 0f),
            Make("shield_01", TraitCategory.Shield, TraitEffectKind.MaxShieldStack,
                "두꺼운 결계", "최대 보호막 수가 1개 증가합니다.", 1f, 0f, 1, 0f),
            Make("shield_02", TraitCategory.Shield, TraitEffectKind.InvincibleDuration,
                "지속의 문양", "보호막 소모 후 무적 시간이 20% 증가합니다.", 0.2f, 0f, 1, 0f),
            Make("shield_03", TraitCategory.Shield, TraitEffectKind.RechargeTime,
                "재충전의 문양", "보호막 충전 시간이 20% 감소합니다.", 0.2f, 0f, 1, 0f),
            Make("shield_04", TraitCategory.Shield, TraitEffectKind.ShieldNova,
                "파열의 고리", "적의 공격으로 보호막이 파괴되면 주변 적을 밀쳐냅니다.", 0f, 0f, 1, 4f),
            Make("shield_05", TraitCategory.Shield, TraitEffectKind.ShieldedMoveSpeed,
                "수호자의 걸음", "보호막이 있는 동안 이동 속도가 15% 증가합니다.", 0.15f, 0f, 1, 0f),
            Make("clease_01", TraitCategory.Cleanse, TraitEffectKind.Immunity,
                "정화의 잔향", "정화 시전 후 2초간 새로운 해로운 상태에 면역이 됩니다.", 0f, 2f, 1, 0f),
            Make("clease_02", TraitCategory.Cleanse, TraitEffectKind.MoveSpeedWindow,
                "가벼워진 몸", "정화로 해로운 상태를 하나 이상 해제하면 3초간 이동 속도가 20% 증가합니다.", 0.2f, 3f, 1, 0f),
            Make("clease_03", TraitCategory.Cleanse, TraitEffectKind.HealOnCleanse,
                "맑은 숨결", "정화로 해로운 상태를 하나 이상 해제하면 최대 체력의 8%를 회복합니다.", 0.08f, 0f, 1, 0f),
            Make("clease_04", TraitCategory.Cleanse, TraitEffectKind.CooldownRefund,
                "순환의 문양", "정화로 해로운 상태를 하나 이상 해제하면 다음 정화의 재사용 대기시간이 25% 감소합니다.", 0.25f, 0f, 1, 0f),
            Make("clease_05", TraitCategory.Cleanse, TraitEffectKind.CleanseNova,
                "정화의 파동", "정화로 해로운 상태를 하나 이상 해제하면 주변 적에게 기본 공격 피해량의 80%를 줍니다.", 0.8f, 0f, 1, 5f),
        };
    }

    private static TraitData Make(string id, TraitCategory category, TraitEffectKind kind,
        string displayName, string description, float magnitude, float duration, int count, float radius)
    {
        TraitData data = ScriptableObject.CreateInstance<TraitData>();
        data.Id = id;
        data.Category = category;
        data.EffectKind = kind;
        data.DisplayName = displayName;
        data.Description = description;
        data.Magnitude = magnitude;
        data.Duration = duration;
        data.Count = Mathf.Max(1, count);
        data.Radius = radius;
        return data;
    }
}
