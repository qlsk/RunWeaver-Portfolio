using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 연쇄(Chain) 룬 워드 연출. 직접 맞은 적에서 연쇄를 받은 적까지 원소 색 점선을 잇고,
/// 받은 적의 발밑에 같은 색 링을 조여들게 한다. 두 적은 잠깐 같은 색으로 번쩍인다.
/// - 그림은 프리팹(ChainLinkFx)의 SpriteRenderer에 할당한 이미지를 쓴다. 점선은 Tiled 모드로 길이만큼 반복한다.
/// - 색은 주력 원소(ReactionElement) 순서대로 _elementColors에서 고른다.
/// - Play로 꺼내 쓰고, 연출이 끝나면 꺼서 다시 쓴다. (프리팹마다 따로 모아 둔다)
/// - 두 적을 따라 움직인다. 적이 사라지면 마지막 위치에 남는다.
/// 시간은 게임 시간으로 흐른다. (일시정지하면 멈춘다)
/// </summary>
[DisallowMultipleComponent]
public class ChainLinkFx : MonoBehaviour
{
    [Header("그림")]
    [Tooltip("점선. SpriteRenderer의 Draw Mode는 Tiled")]
    [SerializeField] private SpriteRenderer _line;
    [Tooltip("연쇄를 받은 적 발밑의 링")]
    [SerializeField] private SpriteRenderer _targetRing;
    [Tooltip("연쇄를 받은 적의 번쩍임")]
    [SerializeField] private SpriteRenderer _targetFlash;
    [Tooltip("직접 맞은 적의 번쩍임")]
    [SerializeField] private SpriteRenderer _sourceFlash;

    [Header("주력 원소별 색 (불, 물, 흙, 바람 순서)")]
    [SerializeField] private Color[] _elementColors =
    {
        new(1f, 0.5f, 0.15f, 1f),
        new(0.3f, 0.62f, 1f, 1f),
        new(0.85f, 0.65f, 0.3f, 1f),
        new(0.45f, 1f, 0.75f, 1f)
    };

    [Header("위치")]
    [Tooltip("적 위치(Transform)에서 점선과 번쩍임 중심까지의 거리")]
    [SerializeField] private Vector2 _bodyOffset = new(0f, 0.1f);
    [Tooltip("적 위치(Transform)에서 발밑 링까지의 거리")]
    [SerializeField] private Vector2 _ringOffset = new(0f, -0.4f);

    [Header("점선")]
    [Tooltip("점선 굵기 (월드 단위)")]
    [SerializeField, Min(0.01f)] private float _lineWidth = 0.125f;
    [Tooltip("점선 양 끝을 적 중심에서 띄우는 거리")]
    [SerializeField, Min(0f)] private float _lineEndInset = 0.25f;
    [Tooltip("점선이 서서히 사라지는 시간")]
    [SerializeField, Min(0.01f)] private float _lineDuration = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _lineAlpha = 1f;

    [Header("발밑 링")]
    [Tooltip("링 크기 (가로, 세로 배율)")]
    [SerializeField] private Vector2 _ringScale = new(0.9f, 0.9f);
    [Tooltip("처음 크기 배율 (여기서 1까지 조여든다)")]
    [SerializeField, Min(0f)] private float _ringStartScale = 1.3f;
    [Tooltip("링이 조여든 뒤 사라지기까지의 시간")]
    [SerializeField, Min(0.01f)] private float _ringDuration = 0.5f;

    [Header("번쩍임")]
    [SerializeField, Min(0f)] private float _targetFlashScale = 1.1f;
    [SerializeField, Min(0.01f)] private float _targetFlashDuration = 0.3f;
    [SerializeField, Min(0f)] private float _sourceFlashScale = 1.1f;
    [SerializeField, Min(0.01f)] private float _sourceFlashDuration = 0.25f;

    private static readonly Dictionary<ChainLinkFx, Stack<ChainLinkFx>> s_pools = new();

    private ChainLinkFx _prefab;
    private Transform _source;
    private Transform _target;
    private Vector3 _sourcePosition;
    private Vector3 _targetPosition;
    private Color _color;
    private float _age;
    private float _lifetime;
    private float _lineSpriteHeight = 1f;

    /// <summary>
    /// 연쇄 연출을 재생한다.
    /// source : 직접 맞은 적 (없으면 sourcePosition에서 시작), target : 연쇄를 받은 적
    /// </summary>
    public static void Play(ChainLinkFx prefab, Transform source, Vector3 sourcePosition, Transform target,
        ReactionElement element)
    {
        if (prefab == null || target == null)
        {
            return;
        }

        ChainLinkFx fx = Take(prefab);
        fx.Begin(source, sourcePosition, target, element);
    }

    private static ChainLinkFx Take(ChainLinkFx prefab)
    {
        if (s_pools.TryGetValue(prefab, out Stack<ChainLinkFx> pool))
        {
            // 씬이 바뀌어 사라진 것은 건너뛴다.
            while (pool.Count > 0)
            {
                ChainLinkFx pooled = pool.Pop();
                if (pooled != null)
                {
                    return pooled;
                }
            }
        }

        ChainLinkFx created = Instantiate(prefab);
        created.name = prefab.name;
        created._prefab = prefab;
        return created;
    }

    private void Begin(Transform source, Vector3 sourcePosition, Transform target, ReactionElement element)
    {
        _source = source;
        _target = target;
        _sourcePosition = source != null ? source.position : sourcePosition;
        _targetPosition = target.position;
        _color = GetElementColor(element);
        _age = 0f;
        _lifetime = Mathf.Max(_lineDuration, _ringDuration, _targetFlashDuration, _sourceFlashDuration);
        _lineSpriteHeight = _line != null && _line.sprite != null ? _line.sprite.bounds.size.y : 1f;

        gameObject.SetActive(true);
        Refresh();
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age >= _lifetime)
        {
            Release();
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        // 두 적을 따라간다. (사라졌거나 꺼진 적은 마지막 위치에 둔다)
        if (_source != null && _source.gameObject.activeInHierarchy)
        {
            _sourcePosition = _source.position;
        }

        if (_target != null && _target.gameObject.activeInHierarchy)
        {
            _targetPosition = _target.position;
        }

        Vector3 from = _sourcePosition + (Vector3)_bodyOffset;
        Vector3 to = _targetPosition + (Vector3)_bodyOffset;

        UpdateLine(from, to);
        UpdateRing();
        UpdateFlash(_targetFlash, to, _targetFlashScale, _targetFlashDuration);
        UpdateFlash(_sourceFlash, from, _sourceFlashScale, _sourceFlashDuration);
    }

    private void UpdateLine(Vector3 from, Vector3 to)
    {
        if (_line == null)
        {
            return;
        }

        Vector3 delta = to - from;
        float distance = delta.magnitude;
        float length = distance - _lineEndInset * 2f;
        float t = Mathf.Clamp01(_age / _lineDuration);
        if (length <= 0.01f || t >= 1f)
        {
            _line.enabled = false;
            return;
        }

        _line.enabled = true;
        Vector3 direction = delta / distance;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _line.transform.SetPositionAndRotation((from + to) * 0.5f, Quaternion.Euler(0f, 0f, angle));
        _line.transform.localScale = new Vector3(1f, _lineWidth / _lineSpriteHeight, 1f);
        _line.size = new Vector2(length, _lineSpriteHeight);
        _line.color = WithAlpha(_color, _lineAlpha * (1f - t));
    }

    private void UpdateRing()
    {
        if (_targetRing == null)
        {
            return;
        }

        float t = Mathf.Clamp01(_age / _ringDuration);
        if (t >= 1f)
        {
            _targetRing.enabled = false;
            return;
        }

        _targetRing.enabled = true;
        // 앞쪽 절반 동안 조여들고, 전체 동안 사라진다.
        float squeeze = Mathf.Lerp(_ringStartScale, 1f, Mathf.Clamp01(t * 2f));
        _targetRing.transform.SetPositionAndRotation(_targetPosition + (Vector3)_ringOffset, Quaternion.identity);
        _targetRing.transform.localScale = new Vector3(_ringScale.x * squeeze, _ringScale.y * squeeze, 1f);
        _targetRing.color = WithAlpha(_color, 1f - t);
    }

    private void UpdateFlash(SpriteRenderer flash, Vector3 position, float scale, float duration)
    {
        if (flash == null)
        {
            return;
        }

        float t = Mathf.Clamp01(_age / duration);
        if (t >= 1f)
        {
            flash.enabled = false;
            return;
        }

        flash.enabled = true;
        flash.transform.SetPositionAndRotation(position, Quaternion.identity);
        flash.transform.localScale = new Vector3(scale, scale, 1f);
        flash.color = WithAlpha(_color, 1f - t);
    }

    private void Release()
    {
        _source = null;
        _target = null;
        gameObject.SetActive(false);

        if (_prefab == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!s_pools.TryGetValue(_prefab, out Stack<ChainLinkFx> pool))
        {
            pool = new Stack<ChainLinkFx>();
            s_pools[_prefab] = pool;
        }

        pool.Push(this);
    }

    private Color GetElementColor(ReactionElement element)
    {
        int index = (int)element;
        if (_elementColors == null || index < 0 || index >= _elementColors.Length)
        {
            return Color.white;
        }

        return _elementColors[index];
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    // 도메인 리로드를 끈 플레이 모드에서도 플레이할 때마다 비운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPools()
    {
        s_pools.Clear();
    }
}
