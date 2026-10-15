using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 영혼 수확 임무의 영혼불(화로) 연출. 불꽃 일렁임은 애니메이션 클립(SoulFlame_StageN, 8프레임 반복)이 하고,
/// 이 컴포넌트는 그 위에 빛을 더한다.
/// - 푸른 광원(Light2D) : 단계가 오를수록 밝고 넓어지며, 불꽃처럼 살짝 흔들린다.
/// - 은은한 후광 : 조명을 받지 않는 부드러운 원을 불꽃 위에 옅게 겹친다.
/// - 영혼불티 : 불꽃에서 작은 빛 알갱이가 피어올라 흩어진다. 단계가 오를수록 많아진다.
/// - 단계 상승 : 불꽃이 확 타오르듯 빛과 후광이 잠깐 커졌다가 돌아온다.
/// 단계는 같은 오브젝트의 Animator "Stage" 값(MissionStageAnimator가 바꾼다)으로 읽는다. 0단계(불 없음)에서는 모두 꺼진다.
/// </summary>
[DisallowMultipleComponent]
public class SoulFlameFx : MonoBehaviour
{
    private static readonly int StageHash = Animator.StringToHash("Stage");
    private const int MaxStage = 5;
    private const int MoteCount = 24;
    private const float ArtPixel = 4f / 546.21f; // 화로 그림의 픽셀 한 칸 (로컬 단위)

    [Header("위치 (로컬)")]
    [Tooltip("불꽃 가운데. 화로 그림 기준 로컬 좌표")]
    [SerializeField] private Vector2 _flameCenter = new(0f, 0.38f);
    [Tooltip("영혼불티가 피어오르는 높이 (로컬)")]
    [SerializeField] private float _moteRise = 0.38f;

    [Header("광원")]
    [SerializeField] private Color _lightColor = new(0.45f, 0.8f, 1f, 1f);
    [SerializeField] private float _lightIntensityPerStage = 0.22f;
    [SerializeField] private float _lightBaseIntensity = 0.35f;
    [SerializeField] private float _lightRadiusBase = 1.4f;
    [SerializeField] private float _lightRadiusPerStage = 0.35f;
    [SerializeField, Range(0f, 0.5f)] private float _flicker = 0.15f;

    [Header("후광")]
    [SerializeField] private Color _haloColor = new(0.35f, 0.78f, 1f, 1f);
    [SerializeField] private float _haloAlphaPerStage = 0.045f;
    [SerializeField] private float _haloSize = 0.55f;

    [Header("단계 상승")]
    [SerializeField, Min(0.01f)] private float _flareDuration = 0.6f;

    private Animator _animator;
    private SpriteRenderer _body;
    private Light2D _light;
    private SpriteRenderer _halo;
    private readonly SpriteRenderer[] _motes = new SpriteRenderer[MoteCount];
    private readonly float[] _moteAge = new float[MoteCount];
    private readonly float[] _moteLife = new float[MoteCount];
    private readonly Vector2[] _moteStart = new Vector2[MoteCount];
    private readonly float[] _moteSway = new float[MoteCount];
    private float _spawnTimer;
    private int _stage;
    private float _shownLevel;
    private float _flare;
    private float _noiseSeed;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _body = GetComponent<SpriteRenderer>();
        _noiseSeed = Random.Range(0f, 100f);
        Build();
    }

    private void Build()
    {
        var lightObject = new GameObject("SoulFlameLight");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = _flameCenter;
        _light = lightObject.AddComponent<Light2D>();
        _light.lightType = Light2D.LightType.Point;
        _light.color = _lightColor;
        _light.intensity = 0f;
        _light.pointLightInnerRadius = 0f;
        _light.falloffIntensity = 0.7f;

        Material unlit = MissionFxSprites.UnlitMaterial;
        _halo = CreateSprite("SoulFlameHalo", MissionFxSprites.SoftCircle, unlit);
        _halo.transform.localPosition = _flameCenter;
        _halo.color = Color.clear;

        for (int i = 0; i < MoteCount; i++)
        {
            _motes[i] = CreateSprite("SoulMote", MissionFxSprites.Pixel, unlit);
            _motes[i].color = Color.clear;
            _moteAge[i] = 1f;
            _moteLife[i] = 1f;
        }
    }

    private SpriteRenderer CreateSprite(string objectName, Sprite sprite, Material material)
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
        int stage = _animator != null ? Mathf.Clamp(_animator.GetInteger(StageHash), 0, MaxStage) : 0;
        if (stage > _stage && stage > 0)
        {
            _flare = 1f; // 단계가 오르면 확 타오른다
        }

        _stage = stage;
        float dt = Time.deltaTime;
        _flare = Mathf.MoveTowards(_flare, 0f, dt / _flareDuration);
        // 단계 값이 바뀌어도 빛 세기는 부드럽게 따라간다.
        _shownLevel = Mathf.MoveTowards(_shownLevel, stage, dt * 3f);
        float flare = _flare * _flare;

        float time = Time.time + _noiseSeed;
        float flicker = 1f - _flicker * Mathf.PerlinNoise(time * 6f, 0.37f);
        float level = _shownLevel;

        if (_light != null)
        {
            _light.intensity = level <= 0.01f ? 0f
                : (_lightBaseIntensity + _lightIntensityPerStage * level) * flicker + flare * 0.9f;
            _light.pointLightOuterRadius = (_lightRadiusBase + _lightRadiusPerStage * level) * (1f + flare * 0.4f);
        }

        int order = _body != null ? _body.sortingOrder : 0;
        int layer = _body != null ? _body.sortingLayerID : 0;
        if (_halo != null)
        {
            Color color = _haloColor;
            color.a = (_haloAlphaPerStage * level) * flicker + flare * 0.25f;
            _halo.color = color;
            float size = _haloSize * (0.7f + 0.06f * level) * (1f + flare * 0.5f);
            _halo.transform.localScale = new Vector3(size * 1.15f, size, 1f);
            _halo.sortingLayerID = layer;
            _halo.sortingOrder = order + 1;
        }

        UpdateMotes(dt, level, order + 2, layer);
    }

    private void UpdateMotes(float dt, float level, int order, int layer)
    {
        // 단계가 오를수록 자주 피어오른다. 0단계에서는 새로 만들지 않는다.
        float rate = level <= 0.01f ? 0f : 2f + level * 2.2f + _flare * 12f;
        _spawnTimer += dt * rate;
        for (int i = 0; i < MoteCount; i++)
        {
            SpriteRenderer mote = _motes[i];
            if (mote == null)
            {
                continue;
            }

            if (_moteAge[i] >= _moteLife[i])
            {
                if (_spawnTimer < 1f)
                {
                    mote.color = Color.clear;
                    continue;
                }

                _spawnTimer -= 1f;
                _moteAge[i] = 0f;
                _moteLife[i] = Random.Range(0.8f, 1.4f);
                _moteStart[i] = _flameCenter + new Vector2(Random.Range(-0.07f, 0.07f), Random.Range(-0.04f, 0.06f));
                _moteSway[i] = Random.Range(0f, 6.28f);
                float pixels = Random.value < 0.3f ? 2f : 1f;
                mote.transform.localScale = Vector3.one * (ArtPixel * pixels);
            }

            _moteAge[i] += dt;
            float t = Mathf.Clamp01(_moteAge[i] / _moteLife[i]);
            float sway = Mathf.Sin(_moteSway[i] + t * 7f) * 0.025f;
            Vector2 position = _moteStart[i] + new Vector2(sway, _moteRise * t);
            // 픽셀 격자에 맞춘다.
            position.x = Mathf.Round(position.x / ArtPixel) * ArtPixel;
            position.y = Mathf.Round(position.y / ArtPixel) * ArtPixel;
            mote.transform.localPosition = position;
            Color color = _haloColor;
            color = Color.Lerp(Color.white, color, t);
            color.a = Mathf.Sin(t * Mathf.PI) * 0.9f;
            mote.color = color;
            mote.sortingLayerID = layer;
            mote.sortingOrder = order;
        }

        _spawnTimer = Mathf.Min(_spawnTimer, 3f);
    }
}
