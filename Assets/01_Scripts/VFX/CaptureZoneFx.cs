using UnityEngine;

/// <summary>
/// 거점 점령 임무의 점령 링 연출.
/// - 켜진 금빛 칸이 은은하게 숨 쉬듯 빛난다. (켜진 칸만 담은 빛 그림을 링 위에 겹친다. _glowSprites)
/// - 가운데에 따뜻한 빛이 은은하게 번진다. 플레이어가 안에 있으면(CaptureZoneBarrier) 칸과 가운데 빛이 더 밝다.
/// - 링 둘레에서 금빛 알갱이가 천천히 피어오른다.
/// - 새 칸이 켜질 때 : 켜진 칸과 가운데 빛이 한 번 번쩍인다.
/// - 완료(마지막 단계) : 링이 크게 빛나고 가운데에서 빛기둥이 솟았다가 사라진다.
/// 단계는 같은 오브젝트의 Animator "Stage" 값(MissionStageAnimator가 바꾼다, 0~8)으로 읽는다.
/// 칸은 맨 위(90도)에서 시작해 시계 방향으로 한 단계에 45도씩 켜진다.
/// </summary>
[DisallowMultipleComponent]
public class CaptureZoneFx : MonoBehaviour
{
    private static readonly int StageHash = Animator.StringToHash("Stage");
    private const int MaxStage = 8;
    private const float DegreesPerStage = 360f / MaxStage;

    [Tooltip("단계 1~8에서 켜진 칸만 담은 빛 그림 (CaptureZone_Glow1~8)")]
    [SerializeField] private Sprite[] _glowSprites;

    [Header("링 모양 (로컬)")]
    [Tooltip("금빛 칸이 지나는 타원의 반지름")]
    [SerializeField] private Vector2 _trackRadius = new(0.86f, 0.523f);

    [Header("빛")]
    [SerializeField] private Color _goldColor = new(1f, 0.82f, 0.45f, 1f);
    [SerializeField] private float _glowAlpha = 0.22f;
    [Tooltip("가운데 빛의 크기 (로컬)")]
    [SerializeField] private Vector2 _centerGlowSize = new(0.75f, 0.45f);

    [Header("알갱이")]
    [Tooltip("그림 픽셀 한 칸의 월드 크기")]
    [SerializeField] private float _pixelSize = 0.022f;
    [Tooltip("초당 피어오르는 알갱이 수")]
    [SerializeField] private float _moteRate = 9f;

    [Header("완료")]
    [SerializeField, Min(0.1f)] private float _completeDuration = 1.4f;
    [SerializeField] private float _pillarHeight = 4f;

    private Animator _animator;
    private SpriteRenderer _body;
    private CaptureZoneBarrier _barrier;
    private SpriteRenderer _glow;
    private SpriteRenderer _centerGlow;
    private SpriteRenderer _pillar;
    private PixelParticles _particles;
    private int _stage;
    private float _pop;
    private float _complete;
    private float _moteTimer;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _body = GetComponent<SpriteRenderer>();
        _barrier = GetComponent<CaptureZoneBarrier>();
        Build();
    }

    private void Build()
    {
        Material unlit = MissionFxSprites.UnlitMaterial;
        _glow = CreateChild("ZoneGlow", null, unlit);
        _centerGlow = CreateChild("ZoneCenterGlow", MissionFxSprites.SoftCircle, unlit);
        _centerGlow.transform.localScale = new Vector3(_centerGlowSize.x, _centerGlowSize.y, 1f);
        _pillar = CreateChild("ZonePillar", PillarSprite, unlit);
        _pillar.color = Color.clear;

        _particles = new GameObject("ZoneParticles").AddComponent<PixelParticles>();
    }

    private SpriteRenderer CreateChild(string objectName, Sprite sprite, Material material)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        if (material != null)
        {
            renderer.sharedMaterial = material;
        }

        return renderer;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        int stage = _animator != null ? Mathf.Clamp(_animator.GetInteger(StageHash), 0, MaxStage) : 0;
        if (stage > _stage)
        {
            _pop = 1f;
            if (stage == MaxStage)
            {
                _complete = 1f;
            }
        }

        _stage = stage;
        _pop = Mathf.MoveTowards(_pop, 0f, dt / 0.45f);
        _complete = Mathf.MoveTowards(_complete, 0f, dt / _completeDuration);

        int order = _body != null ? _body.sortingOrder : 0;
        int layer = _body != null ? _body.sortingLayerID : 0;
        float time = Time.time;
        float breath = 0.5f + 0.5f * Mathf.Sin(time * 2.4f);
        float completeGlow = Mathf.Sin(Mathf.Clamp01(1f - _complete) * Mathf.PI) * (_complete > 0f ? 1f : 0f);
        // 플레이어가 안에 있으면 칸과 가운데 빛이 더 밝다. (CaptureZoneBarrier)
        float presence = _barrier != null ? _barrier.Weight : 1f;

        // 켜진 칸 빛
        _glow.sprite = stage > 0 && _glowSprites != null && _glowSprites.Length >= stage ? _glowSprites[stage - 1] : null;
        Color glow = _goldColor;
        glow.a = stage > 0 ? _glowAlpha * (0.6f + 0.4f * breath) * (0.5f + 0.8f * presence) + _pop * 0.55f + completeGlow * 0.6f : 0f;
        _glow.color = glow;
        Sort(_glow, layer, order + 1);

        // 가운데 빛
        Color center = _goldColor;
        center.a = (0.12f + 0.1f * breath * breath) * (0.4f + 1f * presence) + _pop * 0.25f + completeGlow * 0.5f;
        _centerGlow.color = center;
        Sort(_centerGlow, layer, order + 1);

        // 빛기둥
        if (_complete > 0f)
        {
            float t = 1f - _complete;
            float rise = 1f - (1f - Mathf.Clamp01(t / 0.35f)) * (1f - Mathf.Clamp01(t / 0.35f));
            float height = _pillarHeight * rise / Mathf.Max(0.0001f, transform.lossyScale.y);
            float width = 0.42f * (1f - 0.3f * t);
            _pillar.transform.localPosition = new Vector3(0f, height * 0.5f - 0.05f, 0f);
            _pillar.transform.localScale = new Vector3(width / 0.25f, height, 1f); // 그림 폭이 0.25 단위
            Color pillar = new(1f, 0.9f, 0.65f, Mathf.Sin(t * Mathf.PI) * 0.75f);
            _pillar.color = pillar;
        }
        else
        {
            _pillar.color = Color.clear;
        }

        Sort(_pillar, layer, order + 4);

        // 알갱이 : 링 둘레에서 천천히 피어오른다.
        _particles.SetSorting(layer, order + 4);
        _moteTimer += dt * _moteRate;
        while (_moteTimer >= 1f)
        {
            _moteTimer -= 1f;
            Vector3 origin = transform.TransformPoint(OnTrack(Random.Range(0f, 360f)));
            var velocity = new Vector3(0f, Random.Range(0.4f, 0.6f), 0f);
            Color color = Color.Lerp(_goldColor, Color.white, Random.value * 0.3f);
            _particles.Emit(origin, velocity, 0f, Random.Range(1.4f, 1.9f), color,
                _pixelSize * (Random.value < 0.3f ? 2f : 1f), null, true);
        }
    }

    private Vector2 OnTrack(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians) * _trackRadius.x, Mathf.Sin(radians) * _trackRadius.y);
    }

    private static void Sort(SpriteRenderer renderer, int layer, int order)
    {
        renderer.sortingLayerID = layer;
        renderer.sortingOrder = order;
    }

    private void OnDestroy()
    {
        if (_particles != null)
        {
            _particles.DestroyWhenEmpty = true;
        }
    }

    private static Sprite s_pillarSprite;

    /// <summary>아래가 밝고 위로 갈수록 옅어지며, 가운데가 진하고 양옆이 흐린 세로 빛. 1 x 1 단위 크기.</summary>
    private static Sprite PillarSprite
    {
        get
        {
            if (s_pillarSprite == null)
            {
                const int width = 32;
                const int height = 128;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                for (int y = 0; y < height; y++)
                {
                    float v = (y + 0.5f) / height;
                    float vertical = Mathf.Pow(1f - v, 1.5f) * Mathf.Clamp01(v * 12f);
                    for (int x = 0; x < width; x++)
                    {
                        float u = (x + 0.5f) / width * 2f - 1f;
                        float horizontal = Mathf.Exp(-u * u * 5f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, horizontal * vertical));
                    }
                }

                texture.Apply();
                s_pillarSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
            }

            return s_pillarSprite;
        }
    }
}
