using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 탄환 피해를 기준으로 증기 피해를 계산하고 적중 위치에 증기 구름을 생성한다.
/// </summary>
public class SteamReaction : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private SteamArea _steamAreaPrefab;

    [FoldoutGroup("Steam Settings"), SerializeField] private float _steamRadius = 2f;
    [FoldoutGroup("Steam Settings"), SerializeField] private float _steamDamageMultiplier = 0.2f;
    [FoldoutGroup("Steam Settings"), SerializeField] private float _steamTickInterval = 1f;
    [FoldoutGroup("Steam Settings"), SerializeField] private float _steamDuration = 3f;
    [FoldoutGroup("Steam Settings"), SerializeField] private float _trackingIntervalMultiplier = 10f;

    public void Execute(ReactionContext context)
    {
        float steamTickDamage = context.BulletDamage * _steamDamageMultiplier;

        SteamArea steamArea = Instantiate(_steamAreaPrefab, context.HitPosition, Quaternion.identity);
        steamArea.Initialize(steamTickDamage, _steamRadius, _steamTickInterval, _steamDuration,
            _trackingIntervalMultiplier);
        Sfx.Play(Sfx.ReactSteam);
    }
}