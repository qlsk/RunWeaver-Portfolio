using Sirenix.OdinInspector;
using UnityEngine;

/// 적중 위치에 넓은 범위의 감속과 바깥 방향 밀어내기를 적용하는 범람을 생성한다.
public class DelugeReaction : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private DelugeArea _delugeAreaPrefab;

    [FoldoutGroup("Deluge Settings"), SerializeField] private float _delugeRadius = 3f;
    [FoldoutGroup("Deluge Settings"), SerializeField] private float _delugeDuration = 3f;

    [FoldoutGroup("Slow Settings"), SerializeField] private float _moveSpeedMultiplier = 0.5f;
    [FoldoutGroup("Push Settings"), SerializeField] private float _pushSpeed = 2f;

    /// 적중 위치에 범람을 생성하고 영역 설정과 중심에서 사용할 밀어내기 방향을 전달한다.
    public void Execute(ReactionContext context)
    {
        DelugeArea area = Instantiate(_delugeAreaPrefab, context.HitPosition, Quaternion.identity);
        area.Initialize(_delugeRadius, _delugeDuration, _moveSpeedMultiplier, _pushSpeed, context.BulletDirection);
    }
}
