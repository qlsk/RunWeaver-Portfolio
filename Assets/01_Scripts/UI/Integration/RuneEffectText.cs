using System.Collections.Generic;

/// <summary>
/// 룬 효과 기본 설명. DB 설명이 비어 있을 때 대신 쓴다.
/// 여기에 없는 id는 DB 설명을 그대로 사용한다.
/// </summary>
public static class RuneEffectText
{
    private static readonly Dictionary<string, string> s_descriptions = new()
    {
        { "rune_fire", "탄환에 불 원소를 부여합니다." },
        { "rune_water", "탄환에 물 원소를 부여합니다." },
        { "rune_earth", "탄환에 흙 원소를 부여합니다." },
        { "rune_wind", "탄환에 바람 원소를 부여합니다." },
        { "rune_power", "탄환과 원소 반응의 위력이 50% 증가합니다." },
        { "rune_speed", "탄환 속도가 50% 증가합니다." },
        { "rune_range", "탄환 사거리가 50% 증가합니다." },
        { "rune_grow", "탄환이 날아가는 동안 위력이 점점 커져 최대 200%까지 증가합니다." },
        { "rune_duration", "원소 반응 효과의 지속 시간이 늘어납니다." },
        { "rune_return", "최대 사거리에 닿은 탄환이 플레이어에게 돌아옵니다." },
        { "rune_seek", "탄환이 일정 범위 안의 적을 자동으로 추적합니다." },
        { "rune_pierce", "탄환이 적을 관통합니다. 관통할 때마다 피해량이 5%씩 줄어듭니다(최대 30%)." },
        { "rune_chain", "적중 시 원소 반응이 주변 적에게 한 번 연쇄되며, 연쇄 피해량은 50%입니다." },
        { "rune_fix", "최대 사거리에서 탄환이 10초 동안 그 자리에 머문 뒤 사라집니다." },
        { "rune_orbit", "최대 사거리에서 탄환이 5초 동안 플레이어 주위를 돕니다." },
        { "rune_spread", "탄환 3발을 부채꼴로 발사합니다. 각 탄환의 피해량은 50% 감소합니다." },
        { "rune_multiple", "탄환을 연속 2회 발사합니다. 각 탄환의 피해량은 40% 감소합니다." },
        { "rune_bind", "맞은 적을 1초 동안 속박해 이동을 막습니다." },
        { "rune_absorb", "적중 시 가한 피해의 10%만큼 체력을 회복합니다." },
        { "rune_break", "적중 시 적의 보호막을 파괴하거나 약화 효과를 부여합니다." },
        { "rune_reverse", "첫 적중 후에도 탄환이 사라지지 않고 한 번 방향을 반대로 바꿉니다." },
        { "rune_echo", "원소 반응이 일어난 뒤 잠시 후 같은 반응이 한 번 더 발동합니다." }
    };

    public static string GetDescription(string runeEffectId, string fallback)
    {
        if (!string.IsNullOrEmpty(runeEffectId) && s_descriptions.TryGetValue(runeEffectId, out string description))
        {
            return description;
        }

        return fallback ?? string.Empty;
    }
}
