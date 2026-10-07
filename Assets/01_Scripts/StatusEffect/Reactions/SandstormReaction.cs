using Sirenix.OdinInspector;
using UnityEngine;

/// 적을 끌어당기고 방어력을 낮추며 주기 피해를 주는 모래폭풍을 생성한다.
public class SandstormReaction : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private SandstormArea _sandstormAreaPrefab;

    [FoldoutGroup("Sandstorm Settings"), SerializeField] private float _sandstormRadius = 3f;
    [FoldoutGroup("Sandstorm Settings"), SerializeField] private float _sandstormDuration = 3f;
    [FoldoutGroup("Damage Settings"), SerializeField] private float _tickDamageMultiplier = 0.2f;
    [FoldoutGroup("Damage Settings"), SerializeField] private float _tickInterval = 0.5f;
    [FoldoutGroup("Pull Settings"), SerializeField] private float _pullSpeed = 2f;
    [FoldoutGroup("Defense Settings"), SerializeField] private float _defenseMultiplier = 0.5f;

    /// 적중 위치에 모래폭풍을 생성하고 피해와 당김 및 방어력 감소 설정을 전달한다.
    public void Execute(ReactionContext context)
    {
        SandstormArea area = Instantiate(_sandstormAreaPrefab, context.HitPosition, Quaternion.identity);
        area.Initialize(_sandstormRadius, _sandstormDuration, context.BulletDamage * _tickDamageMultiplier,
            _tickInterval, _pullSpeed, _defenseMultiplier);
    }
}
