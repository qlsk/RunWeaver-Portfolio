using Sirenix.OdinInspector;
using UnityEngine;

/// 적중 위치에 넓은 범위의 연소와 당김을 적용하는 화염폭풍을 생성한다.
public class FirestormReaction : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private FirestormArea _firestormAreaPrefab;

    [FoldoutGroup("Firestorm Settings"), SerializeField] private float _firestormRadius = 3f;
    [FoldoutGroup("Firestorm Settings"), SerializeField] private float _firestormDuration = 3f;

    [FoldoutGroup("Burn Settings"), SerializeField] private float _burnApplyInterval = 0.5f;
    [FoldoutGroup("Burn Settings"), SerializeField] private float _burnDamageMultiplier = 0.2f;
    [FoldoutGroup("Burn Settings"), SerializeField] private float _burnTickInterval = 1f;
    [FoldoutGroup("Burn Settings"), SerializeField] private float _burnDuration = 3f;

    [FoldoutGroup("Pull Settings"), SerializeField] private float _pullSpeed = 1f;

    /// 적중 위치에 화염폭풍을 생성하고 탄환 피해를 기준으로 연소와 당김 설정을 전달한다.
    public void Execute(Vector2 hitPosition, float bulletDamage)
    {
        FirestormArea area = Instantiate(_firestormAreaPrefab, hitPosition, Quaternion.identity);
        area.Initialize(_firestormRadius, _firestormDuration, _burnApplyInterval,
            bulletDamage * _burnDamageMultiplier, _burnTickInterval, _burnDuration, _pullSpeed);
    }
}
