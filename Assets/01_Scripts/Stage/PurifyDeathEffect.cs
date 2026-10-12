using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정화 파동에 닿은 적의 사망 연출. PurificationWave가 Purify로 적에게 붙인다.
/// - 적의 이동·공격·충돌·애니메이션을 멈춘다. (적 스크립트는 고치지 않고 컴포넌트를 끈다)
/// - 번쩍임 : 파동 색으로 밝게 물들며 살짝 커지고, 파동 반대쪽으로 조금 밀려난다.
/// - 소멸 : 위로 늘어나며(가로는 줄고 세로는 늘어남) 떠오르고, 서서히 투명해진다.
/// - 빛 입자 : 몸에서 작은 빛이 위로 흩어진다. (PurifyMotes 프리팹. 움직임은 클립에 있고, 적이 사라져도 끝까지 남는다)
/// 끝나면 적 오브젝트를 없앤다. EnemyHealth의 사망 이벤트를 보내지 않으므로 드롭과 처치 수에 들어가지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class PurifyDeathEffect : MonoBehaviour
{
    private const float FlashDuration = 0.12f;
    private const float DissolveStart = 0.1f;
    private const float DissolveDuration = 0.45f;
    private const float KnockbackDistance = 0.25f;
    private const float KnockbackDuration = 0.2f;
    private const float RiseDistance = 0.2f;
    // PurifyMotes 프리팹이 기준으로 삼는 적 크기(유닛). 적이 더 크면 입자 무리도 그만큼 키운다.
    private const float MoteAreaWidth = 0.8f;
    private const float MoteAreaHeight = 1f;

    private readonly List<SpriteRenderer> _renderers = new();
    private readonly List<Color> _baseColors = new();
    private Vector3 _basePosition;
    private Vector3 _baseScale;
    private Vector2 _pushDirection;
    private Color _tint = Color.white;
    private GameObject _motesPrefab;

    /// <summary>
    /// 적에게 정화 사망 연출을 시작한다. 이미 정화 중이면 무시한다.
    /// </summary>
    /// <param name="enemy">닿은 적</param>
    /// <param name="waveOrigin">파동 중심 (밀려나는 방향 계산용)</param>
    /// <param name="motesPrefab">흩어지는 빛 입자 프리팹 (없으면 입자 없음)</param>
    /// <param name="tint">번쩍일 때의 색 (파동 색)</param>
    public static void Purify(EnemyHealth enemy, Vector3 waveOrigin, GameObject motesPrefab, Color tint)
    {
        if (enemy == null)
        {
            return;
        }

        // 자식(히트박스 등)에 EnemyHealth가 따로 있어도 적 전체에 한 번만 건다.
        GameObject root = FindEnemyRoot(enemy);
        if (root.GetComponent<PurifyDeathEffect>() != null)
        {
            return;
        }

        PurifyDeathEffect effect = root.AddComponent<PurifyDeathEffect>();
        effect.Begin(waveOrigin, motesPrefab, tint);
    }

    private static GameObject FindEnemyRoot(EnemyHealth enemy)
    {
        Transform root = enemy.transform;
        while (root.parent != null && root.parent.GetComponentInParent<EnemyHealth>() != null)
        {
            root = root.parent;
        }

        return root.gameObject;
    }

    private void Begin(Vector3 waveOrigin, GameObject motesPrefab, Color tint)
    {
        _motesPrefab = motesPrefab;
        _tint = tint;
        _tint.a = 1f;

        Vector2 away = (Vector2)(transform.position - waveOrigin);
        _pushDirection = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.up;
        _basePosition = transform.position;
        _baseScale = transform.localScale;

        StopEnemy();

        GetComponentsInChildren(true, _renderers);
        foreach (SpriteRenderer renderer in _renderers)
        {
            _baseColors.Add(renderer.color);
        }

        SpawnMotes();
        StartCoroutine(PlayRoutine());
    }

    /// <summary>움직임, 공격, 충돌, 애니메이션을 멈춘다.</summary>
    private void StopEnemy()
    {
        foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != this)
            {
                behaviour.enabled = false;
            }
        }

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
        {
            col.enabled = false;
        }

        foreach (Rigidbody2D body in GetComponentsInChildren<Rigidbody2D>(true))
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        foreach (Animator animator in GetComponentsInChildren<Animator>(true))
        {
            animator.speed = 0f;
        }
    }

    private IEnumerator PlayRoutine()
    {
        float elapsed = 0f;
        float total = DissolveStart + DissolveDuration;

        while (elapsed < total)
        {
            elapsed += Time.deltaTime;

            float flash = 1f - Mathf.Clamp01(elapsed / FlashDuration);
            float dissolve = Mathf.Clamp01((elapsed - DissolveStart) / DissolveDuration);
            float knock = EaseOut(elapsed / KnockbackDuration);

            // 번쩍일 때 살짝 커졌다가, 소멸하며 가로는 줄고 세로는 늘어난다.
            float pop = 1f + 0.12f * flash;
            transform.localScale = new Vector3(
                _baseScale.x * pop * (1f - 0.4f * dissolve),
                _baseScale.y * pop * (1f + 0.3f * dissolve),
                _baseScale.z);
            transform.position = _basePosition
                + (Vector3)(_pushDirection * (KnockbackDistance * knock))
                + Vector3.up * (RiseDistance * dissolve);

            for (int i = 0; i < _renderers.Count; i++)
            {
                if (_renderers[i] == null)
                {
                    continue;
                }

                // 번쩍이는 동안은 파동 색으로 밝게, 소멸하며 파동 색이 남은 채 투명해진다.
                Color baseColor = _baseColors[i];
                Color color = Color.Lerp(baseColor, _tint, Mathf.Max(flash, 0.6f * dissolve));
                color.a = baseColor.a * (1f - dissolve);
                _renderers[i].color = color;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    // ───────────────────────── 빛 입자 ─────────────────────────

    private void SpawnMotes()
    {
        if (_motesPrefab == null)
        {
            return;
        }

        int sortingLayer = 0;
        int sortingOrder = 0;
        Bounds bounds = new(transform.position, Vector3.one * 0.5f);
        bool hasBounds = false;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                sortingLayer = renderer.sortingLayerID;
                sortingOrder = renderer.sortingOrder;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
                sortingOrder = Mathf.Max(sortingOrder, renderer.sortingOrder);
            }
        }

        // 프리팹은 적 중심을 기준으로 위쪽 절반에 입자가 놓여 있다. 크기는 적에 맞추고, 좌우는 무작위로 뒤집는다.
        Vector3 center = bounds.center;
        center.z = 0f;
        GameObject motes = Instantiate(_motesPrefab, center, Quaternion.identity);
        float scale = Mathf.Clamp(Mathf.Max(bounds.size.x / MoteAreaWidth, bounds.size.y / MoteAreaHeight), 0.6f, 2.5f);
        float flip = Random.value < 0.5f ? -1f : 1f;
        motes.transform.localScale = new Vector3(scale * flip, scale, 1f);

        // 색(투명도 제외)과 그리는 순서만 코드로 정한다. 투명도는 클립이 움직인다.
        foreach (SpriteRenderer renderer in motes.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = sortingOrder + 1;
            renderer.color = new Color(_tint.r, _tint.g, _tint.b, 0f);
        }
    }

    private static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }
}
