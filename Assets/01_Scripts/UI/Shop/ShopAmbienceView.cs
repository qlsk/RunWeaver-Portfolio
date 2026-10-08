using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 배경 분위기 연출.
/// - 등불 : 배경에 그려진 등불 위에 겹친 빛 이미지가 불꽃처럼 일렁인다.
/// - 먼지 : 작은 빛 알갱이들이 천천히 떠올랐다가 사라진다. (실행 중에 만든다)
/// 상점이 열린 동안 게임이 멈춰 있으므로 실제 시간으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
public class ShopAmbienceView : MonoBehaviour
{
    [Header("등불")]
    [SerializeField] private Graphic[] _lanternGlows;
    [SerializeField, Range(0f, 1f)] private float _lanternAlpha = 0.28f;
    [SerializeField, Range(0f, 1f)] private float _lanternFlicker = 0.1f;
    [SerializeField, Min(0f)] private float _lanternFlickerSpeed = 3.5f;
    [Tooltip("일렁일 때 크기 변화")]
    [SerializeField, Range(0f, 0.5f)] private float _lanternScaleFlicker = 0.05f;

    [Header("먼지")]
    [SerializeField] private Sprite _moteSprite;
    [SerializeField] private Color _moteColor = new(1f, 0.88f, 0.62f, 0.55f);
    [SerializeField, Min(0)] private int _moteCount = 16;
    [Tooltip("먼지가 떠다니는 영역 (이 오브젝트 크기 대비 비율. x, y = 시작, z, w = 끝)")]
    [SerializeField] private Vector4 _moteArea = new(0.42f, 0.08f, 1f, 0.9f);
    [SerializeField] private Vector2 _moteSize = new(4f, 9f);
    [SerializeField] private Vector2 _moteLifetime = new(5f, 9f);
    [Tooltip("1초에 떠오르는 거리 (최소, 최대)")]
    [SerializeField] private Vector2 _moteRiseSpeed = new(8f, 22f);
    [SerializeField] private float _moteSway = 14f;

    private class Mote
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Start;
        public float Life;
        public float Time;
        public float Rise;
        public float Phase;
        public float SwaySpeed;
    }

    private readonly List<Mote> _motes = new();
    private Vector3[] _lanternBaseScales;
    private float[] _lanternSeeds;
    private RectTransform _rect;

    private void Awake()
    {
        _rect = transform as RectTransform;

        if (_lanternGlows != null)
        {
            _lanternBaseScales = new Vector3[_lanternGlows.Length];
            _lanternSeeds = new float[_lanternGlows.Length];
            for (int i = 0; i < _lanternGlows.Length; i++)
            {
                _lanternBaseScales[i] = _lanternGlows[i] != null ? _lanternGlows[i].transform.localScale : Vector3.one;
                _lanternSeeds[i] = Random.Range(0f, 100f);
            }
        }

        CreateMotes();
    }

    private void OnEnable()
    {
        // 열 때마다 먼지를 흩어 놓아 처음부터 떠 있는 것처럼 보이게 한다.
        foreach (Mote mote in _motes)
        {
            Respawn(mote, true);
        }
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        UpdateLanterns();

        foreach (Mote mote in _motes)
        {
            mote.Time += deltaTime;
            if (mote.Time >= mote.Life)
            {
                Respawn(mote, false);
            }

            ApplyMote(mote);
        }
    }

    private void UpdateLanterns()
    {
        if (_lanternGlows == null)
        {
            return;
        }

        float time = Time.unscaledTime * _lanternFlickerSpeed;
        for (int i = 0; i < _lanternGlows.Length; i++)
        {
            Graphic glow = _lanternGlows[i];
            if (glow == null)
            {
                continue;
            }

            // 느린 일렁임 + 빠른 떨림을 섞는다.
            float slow = Mathf.PerlinNoise(_lanternSeeds[i], time * 0.35f);
            float fast = Mathf.PerlinNoise(_lanternSeeds[i] + 37f, time * 1.7f);
            float flicker = (slow * 0.7f + fast * 0.3f) * 2f - 1f;

            Color color = glow.color;
            color.a = Mathf.Clamp01(_lanternAlpha + flicker * _lanternFlicker);
            glow.color = color;
            glow.transform.localScale = _lanternBaseScales[i] * (1f + flicker * _lanternScaleFlicker);
        }
    }

    private void CreateMotes()
    {
        for (int i = 0; i < _moteCount; i++)
        {
            var go = new GameObject("Mote", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            var image = go.GetComponent<Image>();
            image.sprite = _moteSprite;
            image.raycastTarget = false;

            var mote = new Mote { Rect = go.GetComponent<RectTransform>(), Image = image };
            mote.Rect.anchorMin = mote.Rect.anchorMax = Vector2.zero;
            _motes.Add(mote);
            Respawn(mote, true);
        }
    }

    private void Respawn(Mote mote, bool randomAge)
    {
        Vector2 size = _rect != null ? _rect.rect.size : new Vector2(1920f, 1080f);
        mote.Start = new Vector2(
            Mathf.Lerp(_moteArea.x, _moteArea.z, Random.value) * size.x,
            Mathf.Lerp(_moteArea.y, _moteArea.w, Random.value) * size.y);
        mote.Life = Random.Range(_moteLifetime.x, Mathf.Max(_moteLifetime.x, _moteLifetime.y));
        mote.Time = randomAge ? Random.Range(0f, mote.Life) : 0f;
        mote.Rise = Random.Range(_moteRiseSpeed.x, Mathf.Max(_moteRiseSpeed.x, _moteRiseSpeed.y));
        mote.Phase = Random.Range(0f, Mathf.PI * 2f);
        mote.SwaySpeed = Random.Range(0.4f, 0.9f);

        float diameter = Random.Range(_moteSize.x, Mathf.Max(_moteSize.x, _moteSize.y));
        mote.Rect.sizeDelta = new Vector2(diameter, diameter);
    }

    private void ApplyMote(Mote mote)
    {
        float t = mote.Time / mote.Life;
        float sway = Mathf.Sin(mote.Time * mote.SwaySpeed + mote.Phase) * _moteSway;
        mote.Rect.anchoredPosition = mote.Start + new Vector2(sway, mote.Rise * mote.Time);

        // 서서히 나타났다가 서서히 사라지고, 중간에 반짝인다.
        float fade = Mathf.Sin(t * Mathf.PI);
        float twinkle = 0.75f + 0.25f * Mathf.Sin(mote.Time * 3.1f + mote.Phase * 2f);
        Color color = _moteColor;
        color.a *= fade * twinkle;
        mote.Image.color = color;
    }
}
