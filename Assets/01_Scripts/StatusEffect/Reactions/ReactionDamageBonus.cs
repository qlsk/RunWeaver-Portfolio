using UnityEngine;

/// <summary>
/// 원소 반응(두 원소가 만나 일어나는 반응) 피해 배율. 기본 1.
/// 보상 특성 attack_03(원소력 강화)을 TraitStatComposer가 넣고, 탄환이 반응을 일으킬 때 곱한다.
/// 반응 피해는 모두 ReactionContext.BulletDamage에서 계산되므로 여기 한 곳만 바꾸면 전체에 적용된다.
/// </summary>
public static class ReactionDamageBonus
{
    public static float Multiplier { get; set; } = 1f;

    // 도메인 리로드를 끈 플레이 모드에서도 플레이할 때마다 되돌린다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        Multiplier = 1f;
    }
}
