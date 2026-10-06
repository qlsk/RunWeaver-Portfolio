using Sirenix.OdinInspector;
using UnityEngine;

/// 적중 위치에 강한 감속과 장시간 노출 시 속박을 적용하는 진흙 영역을 생성한다.
public class MireReaction : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private MireArea _mireAreaPrefab;

    [FoldoutGroup("Mire Settings"), SerializeField] private float _mireRadius = 3f;
    [FoldoutGroup("Mire Settings"), SerializeField] private float _mireDuration = 5f;

    [FoldoutGroup("Slow Settings"), SerializeField] private float _moveSpeedMultiplier = 0.25f;
    [FoldoutGroup("Root Settings"), SerializeField] private float _rootExposureDuration = 2f;

    /// 적중 위치에 진흙 영역을 생성하고 감속과 속박에 필요한 노출 시간 설정을 전달한다.
    public void Execute(Vector2 hitPosition)
    {
        MireArea area = Instantiate(_mireAreaPrefab, hitPosition, Quaternion.identity);
        area.Initialize(_mireRadius, _mireDuration, _moveSpeedMultiplier, _rootExposureDuration);
    }
}
