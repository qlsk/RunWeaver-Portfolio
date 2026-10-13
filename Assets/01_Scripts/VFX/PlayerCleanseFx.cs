using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정화(Cleanse) 시전 연출. 정화 아이콘처럼 초승달 빛이 몸을 휘감아 돌고, 가운데에서 네 갈래 별이 번쩍인다.
/// 작은 별이 흩어지는 동안, 풀린 상태이상의 색을 띤 빛 알갱이가 소용돌이치며 쓸려 나가 사라진다.
/// - 플레이어 아래에 두면 부모에서 Cleanse를 찾아 시전될 때마다 재생한다.
/// - 그림은 자식 SpriteRenderer에 할당한 이미지를 쓴다. 작은 별과 빛 알갱이는 꺼 둔 견본을 복제해 쓴다.
/// - 초승달 묶음(Arcs)의 Y 배율로 캐릭터를 가로로 감싸는 납작한 고리를 만든다.
/// - 고리는 앞쪽(아래 절반)과 뒤쪽(위 절반)을 따로 그린다. 뒤쪽은 몸보다 아래 순서로 그려 몸에 가려진다.
///   (초승달마다 Front, Back 두 장. MAT_SpriteAdditiveClip이 고리 중심 높이에서 잘라 준다)
/// - 작은 별과 빛 알갱이도 고리 뒤쪽에 있을 때는 몸 뒤로 들어간다.
/// 게임 시간으로 흐른다. (일시정지하면 멈춘다)
/// </summary>
[DisallowMultipleComponent]
public class PlayerCleanseFx : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("비워 두면 부모에서 찾는다")]
    [SerializeField] private Cleanse _cleanse;

    [Header("그림")]
    [Tooltip("초승달들의 부모. 이 오브젝트의 크기로 도는 반지름을 정한다")]
    [SerializeField] private Transform _arcs;
    [Tooltip("초승달마다 하나. 이 오브젝트를 돌린다")]
    [SerializeField] private Transform[] _arcPivots;
    [Tooltip("초승달의 앞쪽(고리 아래 절반) 그림. MAT_SpriteAdditiveClip을 쓴다")]
    [SerializeField] private SpriteRenderer[] _arcFronts;
    [Tooltip("초승달의 뒤쪽(고리 위 절반) 그림. 몸보다 아래 순서로 그린다")]
    [SerializeField] private SpriteRenderer[] _arcBacks;
    [SerializeField] private SpriteRenderer _star;
    [SerializeField] private SpriteRenderer _glow;
    [Tooltip("작은 별 견본 (꺼 둔 자식)")]
    [SerializeField] private SpriteRenderer _sparkleTemplate;
    [Tooltip("상태이상 빛 알갱이 견본 (꺼 둔 자식)")]
    [SerializeField] private SpriteRenderer _moteTemplate;

    [Header("위치")]
    [Tooltip("플레이어 위치에서 연출 중심(몸 중심)까지의 거리")]
    [SerializeField] private Vector2 _centerOffset = Vector2.zero;
    [Tooltip("몸 중심에서 초승달이 도는 중심까지의 거리")]
    [SerializeField] private Vector2 _arcOffset = new(0f, -0.05f);
    [Tooltip("고리의 납작함 (세로 배율). 작을수록 가로로 눕는다")]
    [SerializeField, Range(0.1f, 1f)] private float _flatten = 0.32f;

    [Header("몸 뒤로 가리기")]
    [Tooltip("고리 뒤쪽에 있는 빛을 몸 뒤로 보낸다")]
    [SerializeField] private bool _occludeBehind = true;
    [Tooltip("뒤쪽 빛의 정렬 순서. 몸(0)보다 작아야 몸에 가려진다")]
    [SerializeField] private int _backSortingOrder = -3;
    [Tooltip("빛 알갱이가 이만큼 퍼진 뒤부터 뒤쪽으로 보낸다 (처음엔 몸 앞에서 떨어져 나온다)")]
    [SerializeField, Range(0f, 1f)] private float _moteBackAfter = 0.15f;

    [Header("초승달")]
    [Tooltip("도는 반지름 (시작, 끝)")]
    [SerializeField] private Vector2 _arcRadius = new(0.55f, 0.75f);
    [SerializeField, Min(0.01f)] private float _arcExpandDuration = 0.4f;
    [Tooltip("처음 각도 (도). 초승달끼리는 똑같이 나눠 선다")]
    [SerializeField] private float _arcStartAngle = 90f;
    [Tooltip("도는 양 (도). 음수면 시계 방향")]
    [SerializeField] private float _arcSweep = -300f;
    [SerializeField, Min(0.01f)] private float _arcSweepDuration = 0.45f;
    [SerializeField, Min(0f)] private float _arcFadeIn = 0.08f;
    [SerializeField, Min(0.01f)] private float _arcDuration = 0.5f;

    [Header("가운데 별")]
    [SerializeField, Min(0f)] private float _starDelay = 0.06f;
    [SerializeField, Min(0.01f)] private float _starPeak = 0.08f;
    [SerializeField, Min(0.01f)] private float _starFade = 0.24f;
    [Tooltip("별 크기 (시작, 끝)")]
    [SerializeField] private Vector2 _starScale = new(0.6f, 1.2f);
    [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.5f;

    [Header("작은 별")]
    [SerializeField, Min(0)] private int _sparkleCount = 7;
    [Tooltip("몸 중심에서의 거리 (최소, 최대)")]
    [SerializeField] private Vector2 _sparkleRadius = new(0.5f, 0.9f);
    [Tooltip("위아래로 흩어지는 폭")]
    [SerializeField, Min(0f)] private float _sparkleHeight = 0.12f;
    [SerializeField, Min(0f)] private float _sparkleDelay = 0.1f;
    [Tooltip("작은 별마다 늦게 나타나는 정도 (최대)")]
    [SerializeField, Min(0f)] private float _sparkleStagger = 0.15f;
    [SerializeField, Min(0.01f)] private float _sparkleFadeIn = 0.1f;
    [SerializeField, Min(0.01f)] private float _sparkleFadeOut = 0.35f;

    [Header("상태이상 빛 알갱이")]
    [Tooltip("풀린 상태이상 하나당 알갱이 수")]
    [SerializeField, Min(0)] private int _motesPerEffect = 12;
    [Tooltip("처음 퍼진 범위 (가로 표준편차, 세로 반폭)")]
    [SerializeField] private Vector2 _moteSpread = new(0.18f, 0.36f);
    [Tooltip("소용돌이치며 밖으로 나가는 거리")]
    [SerializeField, Min(0f)] private float _moteSwirlDistance = 0.75f;
    [Tooltip("소용돌이 회전량 (라디안)")]
    [SerializeField] private float _moteSwirlTurns = 2.5f;
    [Tooltip("떠오르는 높이")]
    [SerializeField, Min(0f)] private float _moteRise = 0.5f;
    [SerializeField, Min(0.01f)] private float _moteDuration = 0.55f;
    [SerializeField, Min(0.01f)] private float _moteFadeDuration = 0.5f;

    [Header("상태이상 색 (불, 물, 흙, 바람 순서)")]
    [SerializeField] private Color[] _elementColors =
    {
        new(1f, 0.5f, 0.15f, 1f),
        new(0.3f, 0.62f, 1f, 1f),
        new(0.85f, 0.65f, 0.3f, 1f),
        new(0.45f, 1f, 0.75f, 1f)
    };

    private sealed class Sparkle
    {
        public SpriteRenderer Renderer;
        public Vector2 Offset;
        public float Delay;
    }

    private sealed class Mote
    {
        public SpriteRenderer Renderer;
        public Color Color;
        public Vector2 Offset;
        public float Phase;
        public float Speed;
    }

    private readonly List<Sparkle> _sparkles = new();
    private readonly List<Mote> _motes = new();
    private readonly Stack<SpriteRenderer> _sparklePool = new();
    private readonly Stack<SpriteRenderer> _motePool = new();

    private static readonly int ClipYId = Shader.PropertyToID("_ClipY");
    private static readonly int ClipDirId = Shader.PropertyToID("_ClipDir");

    private MaterialPropertyBlock _block;
    private Color _arcFrontColor = Color.white;
    private Color _arcBackColor = Color.white;
    private int _sparkleOrder;
    private int _moteOrder;
    private Color _starColor;
    private Color _glowColor;
    private Color _sparkleColor;
    private Vector3 _starBaseScale;
    private float _moteAlpha = 1f;
    private float _age = -1f;
    private float _lifetime;

    private void Awake()
    {
        if (_cleanse == null)
        {
            _cleanse = GetComponentInParent<Cleanse>();
        }

        _block = new MaterialPropertyBlock();
        _arcFrontColor = FirstColor(_arcFronts);
        _arcBackColor = FirstColor(_arcBacks);
        _sparkleOrder = _sparkleTemplate != null ? _sparkleTemplate.sortingOrder : 0;
        _moteOrder = _moteTemplate != null ? _moteTemplate.sortingOrder : 0;

        _starColor = _star != null ? _star.color : Color.white;
        _starBaseScale = _star != null ? _star.transform.localScale : Vector3.one;
        _glowColor = _glow != null ? _glow.color : Color.white;
        _sparkleColor = _sparkleTemplate != null ? _sparkleTemplate.color : Color.white;
        _moteAlpha = _moteTemplate != null ? _moteTemplate.color.a : 1f;

        if (_sparkleTemplate != null)
        {
            _sparkleTemplate.gameObject.SetActive(false);
        }

        if (_moteTemplate != null)
        {
            _moteTemplate.gameObject.SetActive(false);
        }

        HideAll();
    }

    private void OnEnable()
    {
        if (_cleanse != null)
        {
            _cleanse.Used += Play;
        }
    }

    private void OnDisable()
    {
        if (_cleanse != null)
        {
            _cleanse.Used -= Play;
        }

        Stop();
    }

    /// <summary>연출을 처음부터 재생한다. removed : 풀린 상태이상 (알갱이 색)</summary>
    public void Play(IReadOnlyList<ReactionElement> removed)
    {
        Stop();
        _age = 0f;
        _lifetime = Mathf.Max(_arcDuration, _starDelay + _starPeak + _starFade,
            _sparkleDelay + _sparkleStagger + _sparkleFadeIn + _sparkleFadeOut,
            Mathf.Max(_moteDuration, _moteFadeDuration));

        for (int i = 0; i < _sparkleCount; i++)
        {
            SpriteRenderer renderer = Take(_sparklePool, _sparkleTemplate);
            if (renderer == null)
            {
                break;
            }

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(_sparkleRadius.x, _sparkleRadius.y);
            _sparkles.Add(new Sparkle
            {
                Renderer = renderer,
                Offset = new Vector2(Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius * _flatten + Random.Range(-_sparkleHeight, _sparkleHeight)),
                Delay = Random.Range(0f, _sparkleStagger)
            });
            renderer.sortingOrder = _occludeBehind && Mathf.Sin(angle) > 0f ? _backSortingOrder : _sparkleOrder;
        }

        if (removed != null)
        {
            foreach (ReactionElement element in removed)
            {
                Color color = GetElementColor(element);
                for (int i = 0; i < _motesPerEffect; i++)
                {
                    SpriteRenderer renderer = Take(_motePool, _moteTemplate);
                    if (renderer == null)
                    {
                        break;
                    }

                    _motes.Add(new Mote
                    {
                        Renderer = renderer,
                        Color = color,
                        Offset = new Vector2(Gaussian() * _moteSpread.x, Random.Range(-_moteSpread.y, _moteSpread.y)),
                        Phase = Random.Range(0f, Mathf.PI * 2f),
                        Speed = Random.Range(0.7f, 1.3f)
                    });
                }
            }
        }

        Refresh();
    }

    private void LateUpdate()
    {
        if (_age < 0f)
        {
            return;
        }

        _age += Time.deltaTime;
        if (_age >= _lifetime)
        {
            Stop();
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        float t = _age;
        Vector3 center = transform.position + (Vector3)_centerOffset;

        // 초승달: 캐릭터를 가로로 감싼 납작한 고리 위를 돌며 조금씩 넓어진다.
        Vector3 ringCenter = center + (Vector3)_arcOffset;
        if (_arcs != null)
        {
            float radius = Mathf.Lerp(_arcRadius.x, _arcRadius.y, EaseOut(t / _arcExpandDuration));
            _arcs.position = ringCenter;
            _arcs.localScale = new Vector3(radius, radius * _flatten, 1f);
        }

        float arcAlpha = Bump(t, 0f, _arcFadeIn, _arcDuration);
        float sweep = _arcSweep * EaseOut(t / _arcSweepDuration);
        int arcCount = _arcPivots != null ? _arcPivots.Length : 0;
        for (int i = 0; i < arcCount; i++)
        {
            if (_arcPivots[i] != null)
            {
                _arcPivots[i].localRotation = Quaternion.Euler(0f, 0f, _arcStartAngle + i * 360f / arcCount + sweep);
            }
        }

        // 앞쪽은 고리 중심보다 아래만, 뒤쪽은 위만 그린다. (가리기를 끄면 앞쪽이 전부 그린다)
        UpdateArcs(_arcFronts, _arcFrontColor, arcAlpha, ringCenter.y, _occludeBehind ? -1f : 0f);
        UpdateArcs(_arcBacks, _arcBackColor, _occludeBehind ? arcAlpha : 0f, ringCenter.y, 1f);

        // 가운데 별과 번짐
        float starAlpha = Bump(t, _starDelay, _starDelay + _starPeak, _starDelay + _starPeak + _starFade);
        if (_star != null)
        {
            _star.enabled = starAlpha > 0f;
            float grow = Mathf.Lerp(_starScale.x, _starScale.y, EaseOut((t - _starDelay) / (_starPeak + _starFade * 0.5f)));
            _star.transform.position = center;
            _star.transform.localScale = _starBaseScale * grow;
            _star.color = WithAlpha(_starColor, _starColor.a * starAlpha);
        }

        if (_glow != null)
        {
            _glow.enabled = starAlpha > 0f;
            _glow.transform.position = center;
            _glow.color = WithAlpha(_glowColor, _glowColor.a * starAlpha * _glowAlpha);
        }

        // 작은 별
        foreach (Sparkle sparkle in _sparkles)
        {
            float start = _sparkleDelay + sparkle.Delay;
            float alpha = Bump(t, start, start + _sparkleFadeIn, start + _sparkleFadeIn + _sparkleFadeOut);
            sparkle.Renderer.transform.position = ringCenter + (Vector3)sparkle.Offset;
            sparkle.Renderer.color = WithAlpha(_sparkleColor, _sparkleColor.a * alpha);
        }

        // 상태이상 빛 알갱이: 소용돌이치며 밖으로, 위로 쓸려 나간다.
        float progress = EaseOut(t / _moteDuration);
        float moteFade = Mathf.Clamp01(1f - t / _moteFadeDuration);
        foreach (Mote mote in _motes)
        {
            float angle = mote.Phase + progress * _moteSwirlTurns;
            Vector2 swirl = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * _flatten)
                            * (progress * _moteSwirlDistance * mote.Speed);
            Vector2 position = mote.Offset * 0.8f + swirl + Vector2.up * (progress * _moteRise);
            mote.Renderer.transform.position = center + (Vector3)position;
            bool behind = _occludeBehind && Mathf.Sin(angle) > 0f && progress > _moteBackAfter;
            mote.Renderer.sortingOrder = behind ? _backSortingOrder : _moteOrder;
            mote.Renderer.color = WithAlpha(mote.Color, _moteAlpha * moteFade);
        }
    }

    private void Stop()
    {
        _age = -1f;
        foreach (Sparkle sparkle in _sparkles)
        {
            Release(_sparklePool, sparkle.Renderer);
        }

        foreach (Mote mote in _motes)
        {
            Release(_motePool, mote.Renderer);
        }

        _sparkles.Clear();
        _motes.Clear();
        HideAll();
    }

    private void UpdateArcs(SpriteRenderer[] renderers, Color baseColor, float alpha, float clipY, float clipDir)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (SpriteRenderer arc in renderers)
        {
            if (arc == null)
            {
                continue;
            }

            arc.enabled = alpha > 0f;
            if (!arc.enabled)
            {
                continue;
            }

            arc.color = WithAlpha(baseColor, baseColor.a * alpha);
            arc.GetPropertyBlock(_block);
            _block.SetFloat(ClipYId, clipY);
            _block.SetFloat(ClipDirId, clipDir);
            arc.SetPropertyBlock(_block);
        }
    }

    private void HideAll()
    {
        SetEnabled(_arcFronts, false);
        SetEnabled(_arcBacks, false);

        if (_star != null)
        {
            _star.enabled = false;
        }

        if (_glow != null)
        {
            _glow.enabled = false;
        }
    }

    private static void SetEnabled(SpriteRenderer[] renderers, bool enabled)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }
    }

    private static Color FirstColor(SpriteRenderer[] renderers)
    {
        if (renderers != null)
        {
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    return renderer.color;
                }
            }
        }

        return Color.white;
    }

    private SpriteRenderer Take(Stack<SpriteRenderer> pool, SpriteRenderer template)
    {
        while (pool.Count > 0)
        {
            SpriteRenderer pooled = pool.Pop();
            if (pooled != null)
            {
                pooled.gameObject.SetActive(true);
                return pooled;
            }
        }

        if (template == null)
        {
            return null;
        }

        SpriteRenderer created = Instantiate(template, template.transform.parent);
        created.name = template.name;
        created.gameObject.SetActive(true);
        return created;
    }

    private static void Release(Stack<SpriteRenderer> pool, SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.gameObject.SetActive(false);
        pool.Push(renderer);
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

    /// <summary>a에서 0, b에서 1, c에서 다시 0이 되는 삼각 곡선</summary>
    private static float Bump(float t, float a, float b, float c)
    {
        if (t <= a || t >= c)
        {
            return 0f;
        }

        return t < b ? (t - a) / Mathf.Max(0.0001f, b - a) : (c - t) / Mathf.Max(0.0001f, c - b);
    }

    private static float EaseOut(float value)
    {
        value = Mathf.Clamp01(value);
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }

    private static float Gaussian()
    {
        float u1 = Mathf.Max(0.0001f, Random.value);
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
