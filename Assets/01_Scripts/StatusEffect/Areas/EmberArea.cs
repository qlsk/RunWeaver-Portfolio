using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// 생성 위치의 잔불 범위에 일정 주기로 피해를 적용하고 지속 시간이 끝나면 제거한다.
public class EmberArea : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private Animator _emberEffectPrefab;
    [FoldoutGroup("Area Settings"), SerializeField, Min(0.01f)] private float _radius = 2f;
    [FoldoutGroup("Visual Settings"), SerializeField, Min(0)] private int _emberEffectCount = 8;

    private readonly List<Animator> _emberEffects = new();

    private float _tickDamage;
    private float _tickInterval;

    private float _remainingDuration;
    private float _tickTimer;
    private bool _isInitialized;

    public void Initialize(float tickDamage, float tickInterval, float duration)
    {
        _tickDamage = tickDamage;
        _tickInterval = tickInterval;
        _remainingDuration = duration;
        _tickTimer = 0f;
        _isInitialized = true;
        SpawnEmberEffects();
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        float elapsed = Mathf.Min(Time.deltaTime, _remainingDuration);
        _remainingDuration = Mathf.Max(0f, _remainingDuration - elapsed);
        _tickTimer += elapsed;

        while (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyTickDamage();
        }

        if (_remainingDuration <= 0f)
            Destroy(gameObject);
    }

    /// 장판 범위 안에 불꽃을 배치하고 서로 다른 재생 위치에서 반복 애니메이션을 시작한다.
    private void SpawnEmberEffects()
    {
        foreach (Animator effect in _emberEffects)
        {
            if (effect == null)
                continue;

            effect.gameObject.SetActive(false);
            Destroy(effect.gameObject);
        }
        _emberEffects.Clear();

        if (_emberEffectPrefab == null)
            return;

        for (int i = 0; i < _emberEffectCount; i++)
        {
            Animator effect = Instantiate(_emberEffectPrefab, transform.position,
                _emberEffectPrefab.transform.rotation, transform);
            float effectRadius = GetEmberEffectRadius(effect);
            if (effectRadius > _radius)
            {
                effect.transform.localScale *= _radius / effectRadius;
                effectRadius = _radius;
            }

            float placementRadius = Mathf.Max(0f, _radius - effectRadius);
            Vector2 offset = UnityEngine.Random.insideUnitCircle * placementRadius;
            effect.transform.position = transform.position + new Vector3(offset.x, offset.y, 0f);
            effect.Play(0, 0, UnityEngine.Random.value);
            _emberEffects.Add(effect);
        }
    }

    /// 모든 프레임이 같은 크기와 피벗을 사용하는 불꽃의 사각형 전체를 감싸는 반경을 계산한다.
    private static float GetEmberEffectRadius(Animator effect)
    {
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
        Sprite sprite = renderer.sprite;
        Vector2 size = sprite.rect.size / sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot / sprite.pixelsPerUnit;
        float radius = 0f;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector3 localCorner = new Vector3(
                (corner & 1) == 0 ? -pivot.x : size.x - pivot.x,
                (corner & 2) == 0 ? -pivot.y : size.y - pivot.y, 0f);
            Vector2 offset = renderer.transform.TransformPoint(localCorner) - effect.transform.position;
            radius = Mathf.Max(radius, offset.magnitude);
        }

        return radius;
    }

    private void ApplyTickDamage()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _radius);
        HashSet<EnemyHealth> enemies = new();
        foreach (Collider2D collider in colliders)
        {
            if (!collider.TryGetComponent(out EnemyHealth enemy))
                continue;
            enemies.Add(enemy);
        }

        IEnumerable<EnemyHealth> damageableEnemies =
            enemies.Where(enemy => enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled);
        foreach (EnemyHealth enemy in damageableEnemies)
        {
            enemy.TakeDamage(_tickDamage);
        }
    }
}
