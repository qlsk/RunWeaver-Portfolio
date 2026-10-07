using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// attack 카테고리 핸들러. 발동 지점: 발사(Fired), 적중(OnDamageDealt).
///
/// - attack_04 메아리: 발사 카운터, N회마다 추가탄 1발 (횟수 미포함, Fired 미발행).
/// - attack_05 낙인: 적중 시 표식 3초, 표식 적 추가 피해 15%. 추가 피해의 재귀 호출은 가드로 차단.
/// - attack_01/02 수치형은 TraitStatComposer가 처리한다.
/// </summary>
[DisallowMultipleComponent]
public class AttackTraitHandler : MonoBehaviour
{
    private PlayerAttack _attack;
    private TraitManager _traits;

    private int _fireCount;
    private bool _resolvingBonus;

    private void Start()
    {
        _attack = FindAnyObjectByType<PlayerAttack>();
        _traits = TraitManager.Instance;

        if (_attack != null)
        {
            _attack.Fired -= HandleFired;
            _attack.Fired += HandleFired;
        }

        EnemyHealth.OnDamageDealt -= HandleDamageDealt;
        EnemyHealth.OnDamageDealt += HandleDamageDealt;
    }

    private void OnDestroy()
    {
        if (_attack != null)
        {
            _attack.Fired -= HandleFired;
        }

        EnemyHealth.OnDamageDealt -= HandleDamageDealt;
    }

    private void HandleFired()
    {
        if (_traits == null || _attack == null || !_traits.Has("attack_04"))
        {
            return;
        }

        _fireCount++;
        int every = 5;
        if (_traits.TryGet("attack_04", out TraitData data) && data.Count > 0)
        {
            every = data.Count;
        }

        if (_fireCount % every == 0)
        {
            _attack.FireExtraShot(_traits.GetMagnitude("attack_04", 0.5f));
        }
    }

    private void HandleDamageDealt(EnemyHealth enemy, float damage)
    {
        if (_traits == null || !_traits.Has("attack_05") || enemy == null || _resolvingBonus)
        {
            return;
        }

        float duration = _traits.GetDuration("attack_05", 3f);
        float bonusRatio = _traits.GetMagnitude("attack_05", 0.15f);

        MarkedPrey mark = enemy.GetComponent<MarkedPrey>();
        if (mark != null && mark.IsActive)
        {
            // 표식 적 추가 피해. 재귀 호출 가드.
            _resolvingBonus = true;
            try
            {
                enemy.TakeDamage(damage * bonusRatio);
            }
            finally
            {
                _resolvingBonus = false;
            }
        }

        if (mark == null)
        {
            mark = enemy.gameObject.AddComponent<MarkedPrey>();
        }

        mark.Apply(duration);
    }
}
