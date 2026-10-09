using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 배경을 살아 있게 만든다. (그림은 한 장이고 그 위에 빛 이미지들을 겹친다)
/// - 숨쉬기 : 처음엔 조금 당겨진 화면에서 천천히 물러나고, 이후 아주 느리게 확대 / 이동을 반복한다.
/// - 빛 : 석판 룬, 꽃, 촛불의 빛 이미지 투명도를 바꾼다. (룬과 꽃은 숨 쉬듯, 촛불은 불규칙하게)
/// - 폭포 / 안개 : RawImage의 uv를 흘려 움직인다. (텍스처 Wrap Mode는 Repeat)
/// - 로고 빛 : 로고 모양 마스크 안에서 빛 띠가 지나간다. 처음 한 번, 이후 일정 간격으로.
/// - 결정 연출용으로 룬 빛 세기(Flare)와 추가 확대(ExtraZoom)를 밖에서 정할 수 있다.
/// 실제 시간으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
public class TitleAmbience : MonoBehaviour
{
    [Header("숨쉬기 (배경 묶음)")]
    [SerializeField] private RectTransform _backdrop;
    [SerializeField] private float _introZoom = 1.07f;
    [SerializeField] private float _baseZoom = 1.03f;
    [SerializeField, Min(0.01f)] private float _introZoomDuration = 3f;
    [SerializeField] private float _breathZoom = 0.012f;
    [SerializeField, Min(0.1f)] private float _breathPeriod = 14f;
    [SerializeField] private Vector2 _panAmount = new(7f, 3.5f);
    [SerializeField] private Vector2 _panPeriod = new(18f, 13f);

    [Header("빛")]
    [SerializeField] private Graphic _runeGlow;
    [SerializeField] private Vector2 _runeAlpha = new(0.45f, 0.8f);
    [SerializeField, Min(0.1f)] private float _runePeriod = 3.2f;
    [SerializeField] private Graphic _flowerGlow;
    [SerializeField] private Vector2 _flowerAlpha = new(0.45f, 0.8f);
    [SerializeField, Min(0.1f)] private float _flowerPeriod = 4.1f;
    [SerializeField] private Graphic _candleGlow;
    [SerializeField] private Vector2 _candleAlpha = new(0.5f, 0.95f);
    [SerializeField] private float _candleFlickerSpeed = 7f;

    [Header("흐르는 무늬")]
    [SerializeField] private RawImage _waterfallShimmer;
    [SerializeField] private float _shimmerSpeed = 0.6f;
    [SerializeField] private RawImage[] _mists;
    [Tooltip("안개마다 흐르는 속도 (uv / 초). 순서는 _mists와 같다")]
    [SerializeField] private float[] _mistSpeeds = { 0.012f, -0.007f };

    [Header("로고 빛")]
    [SerializeField] private RectTransform _logoShine;
    [SerializeField] private float _firstShineDelay = 1.2f;
    [SerializeField, Min(0.1f)] private float _shineDuration = 1.4f;
    [SerializeField, Min(0.1f)] private float _shineInterval = 7f;

    private float _startTime;
    private Vector2 _backdropBase;

    /// <summary>룬 빛에 더할 세기 (결정 연출)</summary>
    public float Flare { get; set; }

    /// <summary>숨쉬기 확대에 더할 값 (결정 연출)</summary>
    public float ExtraZoom { get; set; }

    private void Awake()
    {
        if (_backdrop != null)
        {
            _backdropBase = _backdrop.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        _startTime = Time.unscaledTime;
        Update();
    }

    private void Update()
    {
        float t = Time.unscaledTime - _startTime;
        float dt = Time.unscaledDeltaTime;

        if (_backdrop != null)
        {
            float intro = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / _introZoomDuration), 3f);
            float breath = (1f - Mathf.Cos(t * 2f * Mathf.PI / _breathPeriod)) * 0.5f * _breathZoom;
            float zoom = Mathf.Lerp(_introZoom, _baseZoom, intro) + breath + ExtraZoom;
            _backdrop.localScale = new Vector3(zoom, zoom, 1f);
            _backdrop.anchoredPosition = _backdropBase + new Vector2(
                Mathf.Sin(t * 2f * Mathf.PI / _panPeriod.x) * _panAmount.x,
                Mathf.Sin(t * 2f * Mathf.PI / _panPeriod.y) * _panAmount.y);
        }

        SetAlpha(_runeGlow, Wave(_runeAlpha, t, _runePeriod, 0f) + Flare);
        SetAlpha(_flowerGlow, Wave(_flowerAlpha, t, _flowerPeriod, 1f));
        float flicker = Mathf.PerlinNoise(t * _candleFlickerSpeed, 0.37f) * 0.7f + Mathf.PerlinNoise(t * _candleFlickerSpeed * 2.3f, 3.1f) * 0.3f;
        SetAlpha(_candleGlow, Mathf.Lerp(_candleAlpha.x, _candleAlpha.y, flicker));

        if (_waterfallShimmer != null)
        {
            Rect uv = _waterfallShimmer.uvRect;
            uv.y = Mathf.Repeat(uv.y + _shimmerSpeed * dt, 1f);
            _waterfallShimmer.uvRect = uv;
        }

        if (_mists != null)
        {
            for (int i = 0; i < _mists.Length; i++)
            {
                if (_mists[i] == null)
                {
                    continue;
                }

                float speed = _mistSpeeds != null && i < _mistSpeeds.Length ? _mistSpeeds[i] : 0.01f;
                Rect uv = _mists[i].uvRect;
                uv.x = Mathf.Repeat(uv.x + speed * dt, 1f);
                _mists[i].uvRect = uv;
            }
        }

        UpdateLogoShine(t);
    }

    private void UpdateLogoShine(float t)
    {
        if (_logoShine == null || !(_logoShine.parent is RectTransform area))
        {
            return;
        }

        float local = t - _firstShineDelay;
        float phase = local < 0f ? -1f : (local % _shineInterval) / _shineDuration;
        float width = area.rect.width;
        float band = _logoShine.rect.width;
        float x = phase is >= 0f and <= 1f
            ? Mathf.Lerp(-width * 0.5f - band, width * 0.5f + band, phase)
            : -width * 0.5f - band * 2f;
        _logoShine.anchoredPosition = new Vector2(x, _logoShine.anchoredPosition.y);
    }

    private static float Wave(Vector2 range, float t, float period, float offset)
    {
        return Mathf.Lerp(range.x, range.y, 0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI / period + offset));
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }
}
