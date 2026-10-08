using UnityEngine;

/// <summary>
/// 지속 스탯 합성기. 특성·패시브의 수치형 효과를 각 시스템의 배율 슬롯에 밀어넣는다.
///
/// - 쓰는 쪽(PlayerAttack 등)이 조회하는 구조라 보상 쪽은 값을 밀어넣기만 한다.
/// - 쉴드 유지(shield_05)처럼 이벤트가 없는 조건은 매 프레임 폴링으로 반영한다 (싼 연산).
/// - 탄창은 누적 방식이라 목표 보너스와의 차분만 적용한다.
/// </summary>
[DisallowMultipleComponent]
public class TraitStatComposer : MonoBehaviour
{
    private PlayerAttack _attack;
    private PlayerMove _move;
    private Dash _dash;
    private Shield _shield;
    private PlayerHealth _health;
    private PassiveStatManager _passives;
    private TraitManager _traits;
    private DashTraitHandler _dashHandler;
    private CleanseTraitHandler _cleanseHandler;

    private float _baseShieldCoolTime;
    private float _baseInvincibilityDuration;
    private int _appliedMagazineBonus;
    private bool _basesCaptured;

    private void Start()
    {
        _attack = FindAnyObjectByType<PlayerAttack>();
        _move = FindAnyObjectByType<PlayerMove>();
        _dash = FindAnyObjectByType<Dash>();
        _shield = FindAnyObjectByType<Shield>();
        _health = FindAnyObjectByType<PlayerHealth>();
        _passives = PassiveStatManager.Instance;
        _traits = TraitManager.Instance;
        _dashHandler = FindAnyObjectByType<DashTraitHandler>();
        _cleanseHandler = FindAnyObjectByType<CleanseTraitHandler>();

        if (_passives != null)
        {
            _passives.OnStatChanged -= Refresh;
            _passives.OnStatChanged += Refresh;
        }

        if (_traits != null)
        {
            _traits.OnAcquired -= HandleTraitAcquired;
            _traits.OnAcquired += HandleTraitAcquired;
        }

        if (_attack != null)
        {
            // dash_05 첫 공격 버프 소모용. Fired는 발사 후라 다음 발부터 정상화된다.
            _attack.Fired -= HandleFired;
            _attack.Fired += HandleFired;
        }

        CaptureBases();
        Recompute();
    }

    private void OnDestroy()
    {
        if (_attack != null)
        {
            _attack.Fired -= HandleFired;
        }

        if (_passives != null)
        {
            _passives.OnStatChanged -= Refresh;
        }

        if (_traits != null)
        {
            _traits.OnAcquired -= HandleTraitAcquired;
        }
    }

    private void Update()
    {
        Recompute();
    }

    private void HandleTraitAcquired(string id)
    {
        CaptureBases();
        Recompute();
    }

    private void HandleFired()
    {
        if (_dashHandler != null)
        {
            _dashHandler.ConsumeNextAttackBuff();
        }
    }

    private void Refresh(PassiveStatType type)
    {
        Recompute();
    }

    private void CaptureBases()
    {
        if (_basesCaptured)
        {
            return;
        }

        if (_shield != null)
        {
            _baseShieldCoolTime = _shield.ShieldCoolTime;
        }

        if (_health != null)
        {
            _baseInvincibilityDuration = _health.InvincibilityDuration;
        }

        _basesCaptured = _shield != null && _health != null;
    }

    private void Recompute()
    {
        if (_traits == null)
        {
            return;
        }

        float passiveDmg = _passives != null ? _passives.GetMultiplier(PassiveStatType.Damage) : 1f;
        float passiveInterval = _passives != null && _passives.GetMultiplier(PassiveStatType.AttackSpeed) > 0f
            ? 1f / _passives.GetMultiplier(PassiveStatType.AttackSpeed)
            : 1f;
        float passiveMove = _passives != null ? _passives.GetMultiplier(PassiveStatType.MoveSpeed) : 1f;

        // 공격 피해: 패시브 × attack_01 × dash_05 윈도우
        float damageMult = passiveDmg;
        if (_traits.Has("attack_01"))
        {
            damageMult *= 1f + _traits.GetMagnitude("attack_01", 0.2f);
        }

        if (_dashHandler != null && _dashHandler.NextAttackBuffActive)
        {
            damageMult *= 1f + _dashHandler.NextAttackBuffMagnitude;
        }

        if (_attack != null)
        {
            _attack.DamageMultiplier = damageMult;

            float intervalMult = passiveInterval;
            if (_traits.Has("attack_02"))
            {
                intervalMult *= 1f / (1f + _traits.GetMagnitude("attack_02", 0.15f));
            }

            _attack.FireIntervalMultiplier = intervalMult;

            int baseMagazine = Mathf.Max(1, _attack.MagazineSize - _appliedMagazineBonus);
            int targetBonus = _passives != null ? _passives.GetMagazineBonus(baseMagazine) : 0;
            int delta = targetBonus - _appliedMagazineBonus;
            if (delta != 0)
            {
                _attack.AddMagazineBonus(delta);
                _appliedMagazineBonus = targetBonus;
            }
        }

        // 이동 속도: 패시브 × shield_05(유지 중) × clease_02(윈도우)
        float moveMult = passiveMove;
        if (_traits.Has("shield_05") && _shield != null && _shield.CurrentShield > 0)
        {
            moveMult *= 1f + _traits.GetMagnitude("shield_05", 0.15f);
        }

        if (_cleanseHandler != null && _cleanseHandler.MoveSpeedWindowActive)
        {
            moveMult *= 1f + _cleanseHandler.MoveSpeedWindowMagnitude;
        }

        if (_move != null)
        {
            _move.TraitMoveSpeedMultiplier = moveMult;
        }

        // 대시 거리·쿨타임
        if (_dash != null)
        {
            _dash.DashPowerMultiplier = _traits.Has("dash_01") ? 1f + _traits.GetMagnitude("dash_01", 0.2f) : 1f;
            _dash.DashCooldownMultiplier = _traits.Has("dash_02") ? 1f - _traits.GetMagnitude("dash_02", 0.15f) : 1f;
        }

        // 받는 피해: dash_03 윈도우
        if (_health != null)
        {
            bool reducing = _dashHandler != null && _dashHandler.DamageReductionActive;
            _health.DamageTakenMultiplier = reducing ? 1f - _dashHandler.DamageReductionMagnitude : 1f;

            if (_basesCaptured && _traits.Has("shield_02"))
            {
                _health.InvincibilityDuration = _baseInvincibilityDuration * (1f + _traits.GetMagnitude("shield_02", 0.2f));
            }
        }

        // 쉴드 충전 시간
        if (_shield != null && _basesCaptured)
        {
            _shield.ShieldCoolTime = _traits.Has("shield_03")
                ? _baseShieldCoolTime * (1f - _traits.GetMagnitude("shield_03", 0.2f))
                : _baseShieldCoolTime;
        }
    }
}
