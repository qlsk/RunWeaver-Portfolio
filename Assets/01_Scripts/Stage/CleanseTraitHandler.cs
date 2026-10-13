using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// cleanse 카테고리 핸들러. 발동 지점: Cleansed(해제 개수).
///
/// - clease_01: 시전 후 2초 면역 (해제 0개여도 적용).
/// - clease_02: 시전 후 3초 이속 윈도우 (TraitStatComposer가 읽어 반영).
/// - clease_03: 1개 이상 해제 시 최대체력 8% 회복.
/// - clease_04: 1개 이상 해제 시 다음 정화 쿨 25% 감산.
/// - clease_05: 1개 이상 해제 시 주변 적 80% 피해.
/// 연출(TraitFx) : clease_01 초승달 잔향 + 막아 낸 상태이상 튕김, clease_02 이동 잔상 + 바람 줄기,
///   clease_03 회복빛 입자, clease_05 초승달 고리 파동. (clease_04는 HUD 정화 칸이 보여 준다)
/// </summary>
[DisallowMultipleComponent]
public class CleanseTraitHandler : MonoBehaviour
{
    [Header("clease_01 막아 낸 상태이상 연출의 최소 간격 (초)")]
    [SerializeField, Min(0f)] private float _blockFxInterval = 0.2f;

    [Header("clease_02 가벼워진 몸 (연출)")]
    [Tooltip("바람 줄기 간격 (초)")]
    [SerializeField, Min(0.02f)] private float _swiftTrailInterval = 0.06f;
    [Tooltip("이 속도(월드 단위/초)보다 빠를 때만 움직이는 것으로 본다")]
    [SerializeField, Min(0f)] private float _swiftMinSpeed = 0.5f;

    private Cleanse _cleanse;
    private Dash _dash;
    private DashAfterimage _afterimage;
    private PlayerStatusEffectController _statusEffects;
    private PlayerHealth _health;
    private PlayerAttack _attack;
    private TraitManager _traits;
    private Transform _playerTransform;

    private float _moveWindowUntil;
    private float _lastBlockFxTime = -1f;
    private float _swiftTrailTimer;
    private bool _isSwiftEmitting;
    private bool _wasDashing;
    private Vector3 _lastPosition;
    private Rigidbody2D _playerBody;

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
            _lastPosition = _playerTransform.position;
            _playerBody = player.GetComponentInChildren<Rigidbody2D>();
            _afterimage = player.GetComponentInChildren<DashAfterimage>();
        }

        _dash = FindAnyObjectByType<Dash>();
        if (_afterimage == null && _dash != null)
        {
            _afterimage = _dash.GetComponent<DashAfterimage>();
        }

        if (_cleanse != null)
        {
            _cleanse.Cleansed -= HandleCleansed;
            _cleanse.Cleansed += HandleCleansed;
        }

        if (_statusEffects != null)
        {
            _statusEffects.EffectBlocked -= HandleEffectBlocked;
            _statusEffects.EffectBlocked += HandleEffectBlocked;
        }
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            return;
        }

        // 물리 이동은 FixedUpdate에서 일어나므로 위치 차이 대신 Rigidbody 속도로 본다. (프레임마다 0이 끼어 깜빡이지 않게)
        Vector3 position = _playerTransform.position;
        Vector2 velocity = _playerBody != null
            ? _playerBody.linearVelocity
            : (Vector2)(position - _lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPosition = position;

        // clease_02 : 이속 증가 동안 움직이면 잔상과 바람 줄기를 남긴다.
        bool dashing = _dash != null && _dash.IsDashing;
        bool dashJustEnded = _wasDashing && !dashing;
        _wasDashing = dashing;

        float deltaTime = Time.deltaTime;
        bool moving = deltaTime > 0f && velocity.magnitude >= _swiftMinSpeed;
        if (!MoveSpeedWindowActive || !moving)
        {
            StopSwiftEmission();
            return;
        }

        // 대시가 끝나면 대시가 잔상을 꺼 버리므로, 이속 증가 동안에는 다시 켠다.
        if (_afterimage != null && !dashing && (!_isSwiftEmitting || dashJustEnded))
        {
            _afterimage.BeginEmission();
            _isSwiftEmitting = true;
        }

        _swiftTrailTimer -= deltaTime;
        if (_swiftTrailTimer > 0f)
        {
            return;
        }

        _swiftTrailTimer = _swiftTrailInterval;
        TraitFx fx = TraitFx.Instance;
        if (fx != null)
        {
            fx.SpawnSwiftTrail(_playerTransform, velocity);
        }
    }

    private void StopSwiftEmission()
    {
        if (!_isSwiftEmitting)
        {
            return;
        }

        _isSwiftEmitting = false;
        if (_afterimage != null && (_dash == null || !_dash.IsDashing))
        {
            _afterimage.StopEmission();
        }
    }

    private void HandleEffectBlocked(ReactionElement element)
    {
        if (Time.time - _lastBlockFxTime < _blockFxInterval)
        {
            return;
        }

        _lastBlockFxTime = Time.time;
        TraitFx fx = TraitFx.Instance;
        if (fx != null)
        {
            fx.PlayImmunityBlock(_playerTransform);
        }
    }

    private void OnDestroy()
    {
        if (_cleanse != null)
        {
            _cleanse.Cleansed -= HandleCleansed;
        }

        if (_statusEffects != null)
        {
            _statusEffects.EffectBlocked -= HandleEffectBlocked;
        }
    }

    private void HandleCleansed(int clearedCount)
    {
        if (_traits == null)
        {
            return;
        }

        TraitFx fx = TraitFx.Instance;

        // clease_01은 시전 자체에 반응 (해제 0개여도 면역).
        if (_traits.Has("clease_01") && _statusEffects != null)
        {
            float duration = _traits.GetDuration("clease_01", 2f);
            _statusEffects.GrantImmunity(duration);
            if (fx != null)
            {
                fx.PlayImmunity(_playerTransform, duration);
            }
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
            if (fx != null)
            {
                fx.PlayHeal(_playerTransform);
            }
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
            List<Vector3> hitPositions = new();
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                if (Vector2.Distance(enemy.transform.position, center) <= radius)
                {
                    hitPositions.Add(enemy.transform.position);
                    enemy.TakeDamage(damage);
                }
            }

            if (fx != null)
            {
                fx.PlayNova(_playerTransform, radius, hitPositions);
            }
        }
    }
}
