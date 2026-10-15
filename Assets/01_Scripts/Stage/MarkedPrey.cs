using UnityEngine;

/// <summary>
/// 사냥의 낙인(attack_05) 표식. 적 오브젝트에 런타임으로 붙는다. 시간이 끝나면 스스로 떨어진다.
/// - 플레이어 탄환이 적중할 때만 표식을 남기고, 표식이 있는 적에게 주는 탄환 피해를 늘린다.
///   (지속 피해, 반응 장판, 대시·정화 특성 피해에는 붙지 않는다)
/// - 탄환(Bullet)이 피해를 주기 전에 GetHitMultiplier를, 준 뒤에 MarkOnHit을 부른다.
/// - 표식은 중첩되지 않는다. 다시 맞으면 남은 시간만 새로 채운다.
/// - 연출 : 표식이 붙어 있는 동안 발밑 고리와 머리 위 별(TraitFx.AttachMark), 표식 적을 맞히면 번쩍임(TraitFx.PlayMarkHit).
/// </summary>
[DisallowMultipleComponent]
public class MarkedPrey : MonoBehaviour
{
    private const string TraitId = "attack_05";
    private const float DefaultDuration = 3f;
    private const float DefaultBonus = 0.15f;

    private float _remaining;
    private TraitFx.Handle _visual;

    public bool IsActive => _remaining > 0f;

    /// <summary>탄환 피해에 곱할 배율. 특성이 없거나 표식이 없으면 1.</summary>
    public static float GetHitMultiplier(EnemyHealth enemy)
    {
        TraitManager traits = TraitManager.Instance;
        if (enemy == null || traits == null || !traits.Has(TraitId))
        {
            return 1f;
        }

        return enemy.TryGetComponent(out MarkedPrey mark) && mark.IsActive
            ? 1f + traits.GetMagnitude(TraitId, DefaultBonus)
            : 1f;
    }

    /// <summary>
    /// 탄환이 적중한 적에게 표식을 남긴다. 특성이 없거나 적이 죽었으면 아무것도 하지 않는다.
    /// wasMarked : 이번 탄환이 표식 보너스를 받았는지 (번쩍임 연출용)
    /// </summary>
    public static void MarkOnHit(EnemyHealth enemy, Vector2 hitPosition, bool wasMarked)
    {
        TraitManager traits = TraitManager.Instance;
        if (traits == null || !traits.Has(TraitId))
        {
            return;
        }

        if (wasMarked)
        {
            TraitFx fx = TraitFx.Instance;
            if (fx != null)
            {
                fx.PlayMarkHit(hitPosition);
            }
        }

        if (enemy == null || enemy.IsDead)
        {
            return;
        }

        if (!enemy.TryGetComponent(out MarkedPrey mark))
        {
            mark = enemy.gameObject.AddComponent<MarkedPrey>();
        }

        mark.Apply(traits.GetDuration(TraitId, DefaultDuration));
    }

    public void Apply(float duration)
    {
        _remaining = Mathf.Max(_remaining, Mathf.Max(0f, duration));
        if (_remaining > 0f && (_visual == null || !_visual.IsAlive))
        {
            TraitFx fx = TraitFx.Instance;
            if (fx != null)
            {
                _visual = fx.AttachMark(transform);
            }
        }
    }

    // 사망 연출 중에는 컴포넌트가 꺼지므로 그때 표식 연출도 거둔다.
    private void OnDisable()
    {
        _visual?.Release();
        _visual = null;
    }

    private void Update()
    {
        if (_remaining > 0f)
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Destroy(this);
            }
        }
    }
}
