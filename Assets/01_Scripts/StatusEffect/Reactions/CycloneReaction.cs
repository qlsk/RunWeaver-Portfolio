using Sirenix.OdinInspector;
using UnityEngine;

/// 적을 끌어당기며 피해를 주고 종료 시 밀쳐내는 회오리를 생성한다.
public class CycloneReaction : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private CycloneArea _cycloneAreaPrefab;

    [FoldoutGroup("Cyclone Settings"), SerializeField] private float _cycloneRadius = 3f;
    [FoldoutGroup("Cyclone Settings"), SerializeField] private float _cycloneDuration = 3f;
    [FoldoutGroup("Damage Settings"), SerializeField] private float _tickDamageMultiplier = 0.2f;
    [FoldoutGroup("Damage Settings"), SerializeField] private float _tickInterval = 0.5f;
    [FoldoutGroup("Pull Settings"), SerializeField] private float _pullSpeed = 2f;
    [FoldoutGroup("Knockback Settings"), SerializeField] private float _knockbackSpeed = 5f;
    [FoldoutGroup("Knockback Settings"), SerializeField] private float _knockbackDuration = 0.3f;

    /// 적중 위치에 회오리를 생성하고 피해와 당김 및 종료 넉백 설정을 전달한다.
    public void Execute(ReactionContext context)
    {
        CycloneArea area = Instantiate(_cycloneAreaPrefab, context.HitPosition, Quaternion.identity);
        area.Initialize(_cycloneRadius, _cycloneDuration, context.BulletDamage * _tickDamageMultiplier,
            _tickInterval, _pullSpeed, _knockbackSpeed, _knockbackDuration, context.BulletDirection);
    }
}
