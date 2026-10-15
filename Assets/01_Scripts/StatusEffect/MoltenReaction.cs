using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 탄환 피해를 기준으로 용암 피해를 계산하고 적중 위치에 피해와 감속을 주는 용암 지대를 생성한다.
/// </summary>
public class MoltenReaction : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private MoltenArea _moltenAreaPrefab;

    [FoldoutGroup("Molten Settings"), SerializeField] private float _moltenRadius = 2f;
    [FoldoutGroup("Molten Settings"), SerializeField] private float _moltenDamageMultiplier = 0.2f;
    [FoldoutGroup("Molten Settings"), SerializeField] private float _moltenTickInterval = 1f;
    [FoldoutGroup("Molten Settings"), SerializeField] private float _moltenDuration = 3f;
    [FoldoutGroup("Molten Settings"), SerializeField] private float _moveSpeedMultiplier = 0.5f;

    public void Execute(Vector2 hitPosition, float bulletDamage)
    {
        float moltenTickDamage = bulletDamage * _moltenDamageMultiplier;

        MoltenArea moltenArea = Instantiate(_moltenAreaPrefab, hitPosition, Quaternion.identity);
        moltenArea.Initialize(moltenTickDamage, _moltenRadius, _moltenTickInterval, _moltenDuration,
            _moveSpeedMultiplier);
    }
}
