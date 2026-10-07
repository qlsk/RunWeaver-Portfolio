using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// dash 카테고리 핸들러. 발동 지점: DashStarted/DashFinished.
///
/// - dash_03: 종료 후 1초 피해 감소 (TraitStatComposer가 DamageReductionActive를 읽어 반영).
/// - dash_05: 종료 후 2초 첫 공격 버프 (소모는 TraitStatComposer가 Fired 시점에).
/// - dash_04: 대시 구간 위치 기록 + 범위 적 60% 피해 (적당 1회). A 방식 직접 호출.
/// - dash_01/02 수치형은 TraitStatComposer가 처리한다.
/// </summary>
[DisallowMultipleComponent]
public class DashTraitHandler : MonoBehaviour
{
    private Dash _dash;
    private PlayerAttack _attack;
    private TraitManager _traits;
    private Transform _playerTransform;

    private readonly List<Vector3> _path = new();
    private bool _recording;
    private float _damageReductionUntil;
    private float _buffUntil;
    private bool _buffConsumed;

    public bool DamageReductionActive =>
        _traits != null && _traits.Has("dash_03") && Time.time < _damageReductionUntil;

    public float DamageReductionMagnitude =>
        _traits != null ? _traits.GetMagnitude("dash_03", 0.3f) : 0.3f;

    public bool NextAttackBuffActive =>
        _traits != null && _traits.Has("dash_05") && !_buffConsumed && Time.time < _buffUntil;

    public float NextAttackBuffMagnitude =>
        _traits != null ? _traits.GetMagnitude("dash_05", 0.4f) : 0.4f;

    public void ConsumeNextAttackBuff()
    {
        _buffConsumed = true;
    }

    private void Start()
    {
        _dash = FindAnyObjectByType<Dash>();
        _attack = FindAnyObjectByType<PlayerAttack>();
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
    }

    private void OnDestroy()
    {
        if (_dash != null)
        {
            _dash.DashStarted -= HandleDashStarted;
            _dash.DashFinished -= HandleDashFinished;
        }
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

        if (_traits.Has("dash_03"))
        {
            _damageReductionUntil = Time.time + _traits.GetDuration("dash_03", 1f);
        }

        if (_traits.Has("dash_05"))
        {
            _buffUntil = Time.time + _traits.GetDuration("dash_05", 2f);
            _buffConsumed = false;
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
            damage = _attack.BulletDamage * data.magnitude;
            if (data.radius > 0f)
            {
                radius = data.radius;
            }
        }

        HashSet<EnemyHealth> hit = new();
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
                    enemy.TakeDamage(damage);
                    break;
                }
            }
        }
    }
}
