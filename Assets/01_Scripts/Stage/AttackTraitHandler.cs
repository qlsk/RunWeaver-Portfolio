using UnityEngine;

/// <summary>
/// attack 카테고리 핸들러. 발동 지점: 발사(Fired).
///
/// - attack_04 메아리: 발사 카운터, N회마다 추가탄 1발 (횟수 미포함, Fired 미발행). 추가탄에 금빛 혜성 꼬리 연출.
/// - attack_05 낙인: 탄환 적중 때 MarkedPrey가 처리한다. (Bullet이 직접 부른다)
/// - attack_01/02 수치형은 TraitStatComposer가 처리한다.
/// </summary>
[DisallowMultipleComponent]
public class AttackTraitHandler : MonoBehaviour
{
    private PlayerAttack _attack;
    private TraitManager _traits;

    private int _fireCount;

    private void Start()
    {
        _attack = FindAnyObjectByType<PlayerAttack>();
        _traits = TraitManager.Instance;

        if (_attack != null)
        {
            _attack.Fired -= HandleFired;
            _attack.Fired += HandleFired;
        }
    }

    private void OnDestroy()
    {
        if (_attack != null)
        {
            _attack.Fired -= HandleFired;
        }
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
            TraitFx fx = TraitFx.Instance;
            if (fx != null)
            {
                fx.PlayEcho(_attack.FirePoint, _attack.LastExtraBullets);
            }
        }
    }
}
