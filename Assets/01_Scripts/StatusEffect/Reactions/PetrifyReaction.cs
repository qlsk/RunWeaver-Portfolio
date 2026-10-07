using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// 적중 지점 주변 적에게 일정 시간 이동 제한과 방어력 감소를 적용한다.
public class PetrifyReaction : MonoBehaviour
{
    [FoldoutGroup("Petrify Settings"), SerializeField] private float _petrifyRadius = 2f;
    [FoldoutGroup("Petrify Settings"), SerializeField] private float _petrifyDuration = 3f;
    [FoldoutGroup("Petrify Settings"), SerializeField] private float _moveSpeedMultiplier = 0.2f;
    [FoldoutGroup("Petrify Settings"), SerializeField] private bool _root = true;
    [FoldoutGroup("Petrify Settings"), SerializeField] private float _defenseMultiplier = 0.5f;

    /// 적중 위치 주변 살아 있는 적을 중복 없이 조회하고 석화를 부여한다.
    public void Execute(ReactionContext context)
    {
        HashSet<EnemyHealth> enemies = new();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(context.HitPosition, _petrifyRadius))
        {
            if (!collider.TryGetComponent(out EnemyHealth enemy))
                continue;
            if (enemy.IsDead || !enemy.isActiveAndEnabled || !enemies.Add(enemy))
                continue;
            if (!enemy.TryGetComponent(out EnemyStatusEffectController statusEffects) || !statusEffects.isActiveAndEnabled)
                continue;

            statusEffects.ApplyPetrify(_petrifyDuration, _moveSpeedMultiplier, _root, _defenseMultiplier);
        }
    }
}
