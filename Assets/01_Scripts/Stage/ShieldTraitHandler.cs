using UnityEngine;

/// <summary>
/// shield 카테고리 핸들러. 발동 지점: ShieldBroken(실파괴).
///
/// - shield_04: 파괴 시 주변 적 밀쳐내기 (ApplyKnockback 직접 호출).
/// - shield_01/02/03/05 수치형은 TraitStatComposer가 처리한다.
/// </summary>
[DisallowMultipleComponent]
public class ShieldTraitHandler : MonoBehaviour
{
    private Shield _shield;
    private TraitManager _traits;
    private Transform _playerTransform;

    private void Start()
    {
        _shield = FindAnyObjectByType<Shield>();
        _traits = TraitManager.Instance;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }

        if (_shield != null)
        {
            _shield.ShieldBroken -= HandleShieldBroken;
            _shield.ShieldBroken += HandleShieldBroken;
        }
    }

    private void OnDestroy()
    {
        if (_shield != null)
        {
            _shield.ShieldBroken -= HandleShieldBroken;
        }
    }

    private void HandleShieldBroken()
    {
        if (_traits == null || !_traits.Has("shield_04") || _playerTransform == null)
        {
            return;
        }

        float radius = 4f;
        if (_traits.TryGet("shield_04", out TraitData data) && data.radius > 0f)
        {
            radius = data.radius;
        }

        Vector2 center = _playerTransform.position;
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            Vector2 toEnemy = (Vector2)enemy.transform.position - center;
            if (toEnemy.sqrMagnitude > radius * radius)
            {
                continue;
            }

            EnemyStatusEffectController status = enemy.GetComponent<EnemyStatusEffectController>();
            if (status == null)
            {
                status = enemy.GetComponentInChildren<EnemyStatusEffectController>();
            }

            if (status != null)
            {
                Vector2 direction = toEnemy.sqrMagnitude > 0.0001f ? toEnemy.normalized : Vector2.up;
                status.ApplyKnockback(direction, 12f, 0.25f);
            }
        }
    }
}
