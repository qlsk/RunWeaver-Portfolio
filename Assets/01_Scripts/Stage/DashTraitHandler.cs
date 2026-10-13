using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// dash 카테고리 핸들러. 발동 지점: DashStarted/DashFinished.
///
/// - dash_03: 종료 후 1초 피해 감소 (TraitStatComposer가 DamageReductionActive를 읽어 반영).
/// - dash_05: 종료 후 2초 안의 첫 공격 피해 증가. PlayerAttack.NextAttackDamageMultiplier에 넣고,
///   공격하면 PlayerAttack이 1로 되돌린다. 시간 안에 공격하지 않으면 여기서 1로 되돌린다.
/// - dash_04: 대시 구간 위치 기록 + 범위 적 60% 피해 (적당 1회). A 방식 직접 호출.
/// - dash_01/02 수치형은 TraitStatComposer가 처리한다.
/// 연출(TraitFx) : dash_03 장막(피격을 받아 내면 물결), dash_04 궤적 빛줄기 + 초승달 베기, dash_05 총구 별빛 대기 → 강화탄 꼬리.
/// </summary>
[DisallowMultipleComponent]
public class DashTraitHandler : MonoBehaviour
{
    private Dash _dash;
    private PlayerAttack _attack;
    private PlayerHealth _health;
    private TraitManager _traits;
    private Transform _playerTransform;

    private readonly List<Vector3> _path = new();
    private bool _recording;
    private float _damageReductionUntil;
    private float _buffUntil;
    private bool _isBuffPending;
    private TraitFx.Handle _counterVisual;

    public bool DamageReductionActive =>
        _traits != null && _traits.Has("dash_03") && Time.time < _damageReductionUntil;

    public float DamageReductionMagnitude =>
        _traits != null ? _traits.GetMagnitude("dash_03", 0.3f) : 0.3f;

    private void Start()
    {
        _dash = FindAnyObjectByType<Dash>();
        _attack = FindAnyObjectByType<PlayerAttack>();
        _health = FindAnyObjectByType<PlayerHealth>();
        _traits = TraitManager.Instance;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }

        if (_dash != null)
        {
            _dash.DashStarted -= HandleDashStarted;
            _dash.DashStarted += HandleDashStarted;
            _dash.DashFinished -= HandleDashFinished;
            _dash.DashFinished += HandleDashFinished;
        }

        if (_attack != null)
        {
            _attack.Fired -= HandleFired;
            _attack.Fired += HandleFired;
        }

        if (_health != null)
        {
            _health.Damaged -= HandleDamaged;
            _health.Damaged += HandleDamaged;
        }
    }

    private void Update()
    {
        // dash_05 : 시간 안에 공격하지 않았으면 버프를 거둔다. (공격했으면 PlayerAttack이 이미 1로 돌려 놓았다)
        if (_isBuffPending && Time.time >= _buffUntil)
        {
            _isBuffPending = false;
            if (_attack != null)
            {
                _attack.NextAttackDamageMultiplier = 1f;
            }

            ReleaseCounterVisual();
        }
    }

    private void OnDestroy()
    {
        if (_dash != null)
        {
            _dash.DashStarted -= HandleDashStarted;
            _dash.DashFinished -= HandleDashFinished;
        }

        if (_attack != null)
        {
            _attack.Fired -= HandleFired;
        }

        if (_health != null)
        {
            _health.Damaged -= HandleDamaged;
        }

        ReleaseCounterVisual();
    }

    private void HandleFired()
    {
        // dash_05 : 강화된 한 발이 나갔다.
        if (_attack == null || !_attack.LastAttackWasEmpowered)
        {
            return;
        }

        _isBuffPending = false;
        ReleaseCounterVisual();
        TraitFx fx = TraitFx.Instance;
        if (fx != null)
        {
            fx.PlayCounterShot(_attack.FirePoint, _attack.LastAttackBullets);
        }
    }

    private void HandleDamaged(float damage)
    {
        // dash_03 : 장막이 피격을 받아 냈다.
        if (!DamageReductionActive || _playerTransform == null)
        {
            return;
        }

        TraitFx fx = TraitFx.Instance;
        if (fx != null)
        {
            fx.PlayVeilAbsorb(_playerTransform);
        }
    }

    private void ReleaseCounterVisual()
    {
        _counterVisual?.Release();
        _counterVisual = null;
    }

    private void HandleDashStarted()
    {
        if (_traits == null || !_traits.Has("dash_04") || _playerTransform == null)
        {
            return;
        }

        _path.Clear();
        _recording = true;
        StartCoroutine(RecordPath());
    }

    private IEnumerator RecordPath()
    {
        while (_recording && _playerTransform != null)
        {
            _path.Add(_playerTransform.position);
            yield return null;
        }
    }

    private void HandleDashFinished()
    {
        _recording = false;

        if (_traits == null)
        {
            return;
        }

        TraitFx fx = TraitFx.Instance;

        if (_traits.Has("dash_03"))
        {
            float duration = _traits.GetDuration("dash_03", 1f);
            _damageReductionUntil = Time.time + duration;
            if (fx != null && _playerTransform != null)
            {
                fx.PlayVeil(_playerTransform, duration);
            }
        }

        if (_traits.Has("dash_05") && _attack != null)
        {
            _buffUntil = Time.time + _traits.GetDuration("dash_05", 2f);
            _attack.NextAttackDamageMultiplier = 1f + _traits.GetMagnitude("dash_05", 0.4f);
            _isBuffPending = true;
            ReleaseCounterVisual();
            if (fx != null)
            {
                _counterVisual = fx.PlayCounterReady(_attack.FirePoint);
            }
        }

        if (_traits.Has("dash_04") && _attack != null)
        {
            StrikePath();
        }

        _path.Clear();
    }

    private void StrikePath()
    {
        float radius = 1.5f;
        float damage = _attack.BulletDamage * 0.6f;
        if (_traits.TryGet("dash_04", out TraitData data))
        {
            damage = _attack.BulletDamage * data.Magnitude;
            if (data.Radius > 0f)
            {
                radius = data.Radius;
            }
        }

        HashSet<EnemyHealth> hit = new();
        List<Vector3> hitPositions = new();
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || !hit.Add(enemy))
            {
                continue;
            }

            foreach (Vector3 point in _path)
            {
                if (Vector2.Distance(enemy.transform.position, point) <= radius)
                {
                    hitPositions.Add(enemy.transform.position);
                    enemy.TakeDamage(damage);
                    break;
                }
            }
        }

        TraitFx fx = TraitFx.Instance;
        if (fx != null && _path.Count > 0)
        {
            Vector3 end = _playerTransform != null ? _playerTransform.position : _path[_path.Count - 1];
            fx.PlayPathStrike(_path[0], end, hitPositions);
        }
    }
}
