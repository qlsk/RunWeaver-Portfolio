using System.Collections;
using UnityEngine;

/// <summary>
/// cleanse 카테고리 핸들러. 발동 지점: Cleansed(해제 개수).
///
/// - clease_01: 시전 후 2초 면역 (해제 0개여도 적용).
/// - clease_02: 시전 후 3초 이속 윈도우 (TraitStatComposer가 읽어 반영).
/// - clease_03: 1개 이상 해제 시 최대체력 8% 회복.
/// - clease_04: 1개 이상 해제 시 다음 정화 쿨 25% 감산.
/// - clease_05: 1개 이상 해제 시 주변 적 80% 피해.
/// </summary>
[DisallowMultipleComponent]
public class CleanseTraitHandler : MonoBehaviour
{
    private Cleanse _cleanse;
    private PlayerStatusEffectController _statusEffects;
    private PlayerHealth _health;
    private PlayerAttack _attack;
    private TraitManager _traits;
    private Transform _playerTransform;

    private float _moveWindowUntil;

    public bool MoveSpeedWindowActive =>
        _traits != null && _traits.Has("clease_02") && Time.time < _moveWindowUntil;

    public float MoveSpeedWindowMagnitude =>
        _traits != null ? _traits.GetMagnitude("clease_02", 0.2f) : 0.2f;

    private void Start()
    {
        _cleanse = FindAnyObjectByType<Cleanse>();
        _statusEffects = FindAnyObjectByType<PlayerStatusEffectController>();
        _health = FindAnyObjectByType<PlayerHealth>();
        _attack = FindAnyObjectByType<PlayerAttack>();
        _traits = TraitManager.Instance;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }

        if (_cleanse != null)
        {
            _cleanse.Cleansed -= HandleCleansed;
            _cleanse.Cleansed += HandleCleansed;
        }
    }

    private void OnDestroy()
    {
        if (_cleanse != null)
        {
            _cleanse.Cleansed -= HandleCleansed;
        }
    }

    private void HandleCleansed(int clearedCount)
    {
        if (_traits == null)
        {
            return;
        }

        // clease_01은 시전 자체에 반응 (해제 0개여도 면역).
        if (_traits.Has("clease_01") && _statusEffects != null)
        {
            _statusEffects.GrantImmunity(_traits.GetDuration("clease_01", 2f));
        }

        // 아래는 1개 이상 해제했을 때만 발동.
        if (clearedCount <= 0)
        {
            return;
        }

        if (_traits.Has("clease_02"))
        {
            _moveWindowUntil = Time.time + _traits.GetDuration("clease_02", 3f);
        }

        if (_traits.Has("clease_03") && _health != null)
        {
            _health.Heal(_health.MaxHealth * _traits.GetMagnitude("clease_03", 0.08f));
        }

        if (_traits.Has("clease_04") && _cleanse != null)
        {
            _cleanse.RefundCooldown(_traits.GetMagnitude("clease_04", 0.25f));
        }

        if (_traits.Has("clease_05") && _attack != null && _playerTransform != null)
        {
            float radius = 5f;
            float ratio = 0.8f;
            if (_traits.TryGet("clease_05", out TraitData data))
            {
                ratio = data.Magnitude;
                if (data.Radius > 0f)
                {
                    radius = data.Radius;
                }
            }

            float damage = _attack.BulletDamage * ratio;
            Vector2 center = _playerTransform.position;
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                if (Vector2.Distance(enemy.transform.position, center) <= radius)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }
    }
}
