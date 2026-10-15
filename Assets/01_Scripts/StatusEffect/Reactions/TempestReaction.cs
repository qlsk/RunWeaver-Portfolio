using Sirenix.OdinInspector;
using UnityEngine;

/// 적중 위치에 적을 중심으로 끌어당기며 주기 피해를 주는 폭풍우를 생성한다.
public class TempestReaction : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private TempestArea _tempestAreaPrefab;

    [FoldoutGroup("Tempest Settings"), SerializeField] private float _tempestRadius = 3f;
    [FoldoutGroup("Tempest Settings"), SerializeField] private float _tempestDuration = 3f;

    [FoldoutGroup("Damage Settings"), SerializeField] private float _tickDamageMultiplier = 0.2f;
    [FoldoutGroup("Damage Settings"), SerializeField] private float _tickInterval = 0.5f;
    [FoldoutGroup("Pull Settings"), SerializeField] private float _pullSpeed = 2f;

    /// 적중 위치에 폭풍우를 생성하고 탄환 피해를 기준으로 주기 피해와 당김 설정을 전달한다.
    public void Execute(ReactionContext context)
    {
        TempestArea area = Instantiate(_tempestAreaPrefab, context.HitPosition, Quaternion.identity);
        area.Initialize(_tempestRadius, _tempestDuration, context.BulletDamage * _tickDamageMultiplier,
            _tickInterval, _pullSpeed);
        Sfx.Play(Sfx.ReactTempest);
    }
}
