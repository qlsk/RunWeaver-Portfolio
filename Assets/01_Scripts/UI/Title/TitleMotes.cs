using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면에 떠오르는 푸른 빛 알갱이. (꽃, 물가, 석판 룬 주변)
/// 시작할 때 알갱이 이미지를 만들어 두고 계속 다시 쓴다.
/// 영역은 이 오브젝트 크기에 대한 비율(0~1, 왼쪽 아래 기준)로 정한다.
/// Attract(0~1)를 올리면 알갱이가 목표 지점(석판 룬)으로 빨려 들어간다. (결정 연출)
/// 실제 시간으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class TitleMotes : MonoBehaviour
{
    [Serializable]
    private class Area
    {
        [Tooltip("x, y, 너비, 높이 (화면 비율, 왼쪽 아래 기준)")]
        public Rect rect = new(0f, 0f, 0.2f, 0.2f);
        public int count = 12;
        public Color color = new(0.6f, 0.78f, 1f, 1f);
        [Tooltip("위로 올라가는 속도 범위 (픽셀 / 초)")]
        public Vector2 riseSpeed = new(10f, 28f);
        [Tooltip("최대 투명도")]
        public float alpha = 0.9f;
    }

    private struct Mote
    {
        public RectTransform rect;
        public Image image;
        public Area area;
        public Vector2 start;
        public Vector2 velocity;
        public float age;
        public float life;
        public float phase;
    }

    [SerializeField] private Sprite _sprite;
    [SerializeField] private Area[] _areas;
    [SerializeField] private Vector2 _sizeRange = new(12f, 18f);
    [SerializeField] private Vector2 _lifeRange = new(4f, 8f);
    [SerializeField] private float _wobble = 7f;
    [SerializeField] private float _twinkleSpeed = 5f;

    [Header("빨려 들어가기")]
    [SerializeField] private RectTransform _attractTarget;

    private Mote[] _motes;
    private RectTransform _rect;

    /// <summary>목표 지점으로 끌려가는 정도 (0~1)</summary>
    public float Attract { get; set; }

    private void Awake()
    {
        _rect = (RectTransform)transform;
        int total = 0;
        foreach (Area area in _areas)
        {
            total += area.count;
        }

        _motes = new Mote[total];
        int n = 0;
        foreach (Area area in _areas)
        {
            for (int i = 0; i < area.count; i++)
            {
                GameObject go = new("Mote", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                Image image = go.GetComponent<Image>();
                image.sprite = _sprite;
                image.raycastTarget = false;
                RectTransform rect = (RectTransform)go.transform;
                float size = UnityEngine.Random.Range(_sizeRange.x, _sizeRange.y);
                rect.sizeDelta = new Vector2(size, size);
                _motes[n] = new Mote { rect = rect, image = image, area = area };
                Respawn(ref _motes[n], UnityEngine.Random.Range(0f, 1f));
                n++;
            }
        }
    }

    private void Update()
    {
        if (_motes == null)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;
        Vector2 target = GetTargetPosition();
        float attract = Mathf.Clamp01(Attract);

        for (int i = 0; i < _motes.Length; i++)
        {
            ref Mote mote = ref _motes[i];
            mote.age += dt;
            if (mote.age >= mote.life)
            {
                Respawn(ref mote, 0f);
            }

            float k = mote.age / mote.life;
            Vector2 position = mote.start + mote.velocity * mote.age +
                               new Vector2(Mathf.Sin(mote.age * 1.3f + mote.phase) * _wobble, 0f);
            position = Vector2.Lerp(position, target, attract);
            mote.rect.anchoredPosition = position;

            float twinkle = 0.6f + 0.4f * Mathf.Sin(now * _twinkleSpeed + mote.phase);
            Color color = mote.area.color;
            color.a = Mathf.Sin(k * Mathf.PI) * twinkle * mote.area.alpha * (1f - attract * 0.3f);
            mote.image.color = color;
        }
    }

    private void Respawn(ref Mote mote, float startAge01)
    {
        Rect bounds = _rect.rect;
        Rect area = mote.area.rect;
        float x = Mathf.Lerp(area.xMin, area.xMax, UnityEngine.Random.value);
        float y = Mathf.Lerp(area.yMin, area.yMax, UnityEngine.Random.value);
        mote.start = new Vector2(bounds.xMin + x * bounds.width, bounds.yMin + y * bounds.height);
        mote.velocity = new Vector2(UnityEngine.Random.Range(-6f, 6f),
            UnityEngine.Random.Range(mote.area.riseSpeed.x, mote.area.riseSpeed.y));
        mote.life = UnityEngine.Random.Range(_lifeRange.x, _lifeRange.y);
        mote.age = mote.life * startAge01;
        mote.phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
    }

    private Vector2 GetTargetPosition()
    {
        if (_attractTarget == null)
        {
            return Vector2.zero;
        }

        Vector3 world = _attractTarget.TransformPoint(_attractTarget.rect.center);
        return _rect.InverseTransformPoint(world);
    }
}
