using System;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private float _range;
    private float _travelDistance;
    [Header("총구 발사 이펙트")]
    [SerializeField] private ParticleSystem _castEffectPrefab;
    [SerializeField, Min(0.01f)] private float _castEffectScale = 1f;
    [SerializeField, Min(0.1f)] private float _castEffectLifetime = 3f;

    [Header("룬 시스템 연결")]
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private LayerMask _enemyLayers = ~0;

    private BulletStats _stats;
    private BulletEffectConfig _effects;
    private BulletRuntime _runtime;
    private Transform _owner;
    private Vector2 _direction;
    private bool _initialized;
    private IReadOnlyList<RuneEffectDefinition> _activatedRuneEffects;

    public event Action<EnemyHealth, float> HitEffectsRequested;
    public event Action<EnemyHealth> ElementReactionRequested;
    public event Action<EnemyHealth> ChainRequested;
    public Transform Owner => _owner;
    public BulletPhase Phase => _runtime == null ? BulletPhase.Ended : _runtime.Phase;

    public void PlayCastEffect(Transform muzzle)
    {
        if (_castEffectPrefab == null || muzzle == null) return;

        ParticleSystem effect = Instantiate(_castEffectPrefab, muzzle, false);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        effect.transform.localScale *= _castEffectScale;

        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.stopAction = ParticleSystemStopAction.None;
            var trails = particles.trails;
            trails.worldSpace = false;
        }

        effect.gameObject.SetActive(true);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
        Destroy(effect.gameObject, _castEffectLifetime);
    }

    /// <summary>탄환의 기본 능력치와 발사 방향, 주력 원소를 설정한다.</summary>
    public void Initialize(float damage, float speed, float range, Vector2 direction,
        IReadOnlyList<RuneEffectDefinition> activatedRuneEffects)
    {
        _damage = damage;
        _speed = speed;
        _range = range;
        _direction = direction.normalized;
        _activatedRuneEffects = activatedRuneEffects;

        _travelDistance = 0f;
        _initialized = true;
    }

    /// <summary>발사 방향으로 이동하고 최대 사거리에 도달하면 소멸한다.</summary>
    private void Update()
    {
        if (!_initialized)
            return;

        float remainingDistance = _range - _travelDistance;
        float moveDistance = Mathf.Min(_speed * Time.deltaTime, remainingDistance);

        transform.position += (Vector3)(_direction * moveDistance);
        _travelDistance += moveDistance;

        if (_travelDistance >= _range)
            End();
    }

    /// <summary>살아 있는 적에게 적중하면 피해를 적용하고 소멸한다.</summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_initialized)
            return;

        var enemyHealth = collision.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null || enemyHealth.IsDead || !enemyHealth.isActiveAndEnabled)
            return;

        // Destroy가 처리되기 전 중복 충돌로 피해가 반복되는 것을 막는다.
        _initialized = false;
        enemyHealth.TakeDamage(_damage);

        const int SubElementIndex = RuneEffectLoadout.ElementRuneEffectSlotIndex;
        bool hasSubElement = _activatedRuneEffects.Count > SubElementIndex &&
                             _activatedRuneEffects[SubElementIndex].Category == RuneEffectCategory.Element;
        if (!enemyHealth.IsDead && !hasSubElement)
        {
            var statusEffects = enemyHealth.GetComponent<EnemyStatusEffectController>();
            string elementName = _activatedRuneEffects[RuneEffectLoadout.PrimaryElementSlotIndex].EnglishName;
            if (!Enum.TryParse(elementName, out ReactionElement element))
                throw new InvalidOperationException($"Invalid element: {element}");
            statusEffects.ApplyElementEffect(element, _damage, _direction);
        }

        Destroy(gameObject);
    }

    /// <summary>탄환의 동작을 중지하고 게임 오브젝트를 제거한다.</summary>
    private void End()
    {
        _initialized = false;
        Destroy(gameObject);
    }
}