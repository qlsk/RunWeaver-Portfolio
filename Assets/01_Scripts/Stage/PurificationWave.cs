using System;
using UnityEngine;

/// <summary>
/// 정화 파동 VFX. 시작 위치에서 빛 고리가 빠르게 퍼져 나간다.
/// - 고리(_ring) : 반지름에 맞춰 커지고, _fadeStartRadius부터 최대 반지름까지 서서히 사라진다.
/// - 안쪽 빛(_fill) : 고리가 지나간 바닥에 남는 은은한 빛. 시간이 지나며 옅어진다.
/// - 시작 빛(_flash) : 시작 위치에서 잠깐 번쩍인다.
/// 판정은 CurrentRadius / IsInside로 확인한다. (적 처리 연결은 아직 하지 않았다)
/// 게임 시간(Time.deltaTime)으로 돌아가므로 일시정지 중에는 멈춘다.
/// </summary>
[DisallowMultipleComponent]
public class PurificationWave : MonoBehaviour
{
    [Header("구성")]
    [SerializeField] private SpriteRenderer _ring;
    [SerializeField] private SpriteRenderer _fill;
    [SerializeField] private SpriteRenderer _flash;
    [Tooltip("고리 이미지에서 빛이 가장 밝은 곳의 반지름 비율 (이미지 반지름 = 1). 판정 반지름과 보이는 고리를 맞춘다.")]
    [SerializeField, Range(0.1f, 1f)] private float _ringVisualRatio = 0.72f;

    [Header("퍼짐")]
    [Tooltip("최대 반지름(유닛). 여기까지 퍼지면 끝난다. 화면 밖 적까지 닿도록 넉넉히 둔다.")]
    [SerializeField, Min(0.1f)] private float _maxRadius = 18f;
    [Tooltip("퍼지는 속도(초당 유닛)")]
    [SerializeField, Min(0.1f)] private float _speed = 9f;
    [Tooltip("시작 직후 순간적으로 더 퍼지는 거리(유닛). 처음에 터지듯 퍼지는 느낌을 준다.")]
    [SerializeField, Min(0f)] private float _burstRadius = 1.6f;
    [SerializeField, Min(0.1f)] private float _burstSharpness = 8f;

    [Header("고리")]
    [SerializeField] private Color _ringColor = new(1f, 0.86f, 0.55f, 1f);
    [Tooltip("이 반지름부터 최대 반지름까지 고리가 서서히 사라진다")]
    [SerializeField, Min(0f)] private float _fadeStartRadius = 10f;

    [Header("안쪽 빛")]
    [SerializeField] private Color _fillColor = new(1f, 0.86f, 0.55f, 0.18f);
    [Tooltip("안쪽 빛이 완전히 사라지는 시간(초)")]
    [SerializeField, Min(0.01f)] private float _fillDuration = 1.6f;

    [Header("시작 빛")]
    [SerializeField] private Color _flashColor = new(1f, 0.95f, 0.8f, 0.9f);
    [SerializeField, Min(0f)] private float _flashRadius = 2.5f;
    [SerializeField, Min(0.01f)] private float _flashDuration = 0.45f;

    [Header("테스트")]
    [Tooltip("켜면 시작할 때 바로 재생한다")]
    [SerializeField] private bool _playOnStart;
    [Tooltip("켜면 재생이 끝난 뒤 오브젝트를 없앤다")]
    [SerializeField] private bool _destroyOnFinish;

    private float _elapsed;

    /// <summary>현재 고리 반지름(유닛)</summary>
    public float CurrentRadius { get; private set; }

    public bool IsPlaying { get; private set; }

    /// <summary>매 프레임 반지름이 바뀔 때 (반지름)</summary>
    public event Action<float> RadiusChanged;

    /// <summary>최대 반지름까지 퍼져 끝났을 때</summary>
    public event Action Finished;

    private void Awake()
    {
        SetVisible(false);
    }

    private void Start()
    {
        if (_playOnStart)
        {
            Play();
        }
    }

    /// <summary>지금 위치에서 파동을 시작한다.</summary>
    public void Play()
    {
        _elapsed = 0f;
        CurrentRadius = 0f;
        IsPlaying = true;
        SetVisible(true);
        Apply();
    }

    /// <summary>지정한 위치로 옮겨 파동을 시작한다.</summary>
    public void Play(Vector3 position)
    {
        transform.position = position;
        Play();
    }

    /// <summary>위치가 지금 고리 안쪽(이미 파동이 지나간 곳)인지 (2D 거리)</summary>
    public bool IsInside(Vector3 worldPosition)
    {
        Vector2 offset = worldPosition - transform.position;
        return IsPlaying && offset.sqrMagnitude <= CurrentRadius * CurrentRadius;
    }

    [ContextMenu("Play")]
    private void PlayFromContextMenu()
    {
        Play();
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        CurrentRadius = Mathf.Min(_maxRadius,
            _speed * _elapsed + _burstRadius * (1f - Mathf.Exp(-_elapsed * _burstSharpness)));
        Apply();
        RadiusChanged?.Invoke(CurrentRadius);

        if (CurrentRadius >= _maxRadius)
        {
            Finish();
        }
    }

    private void Finish()
    {
        IsPlaying = false;
        SetVisible(false);
        Finished?.Invoke();

        if (_destroyOnFinish)
        {
            Destroy(gameObject);
        }
    }

    private void Apply()
    {
        float radius = CurrentRadius;

        // 고리 : 이미지에서 밝은 부분이 반지름에 오도록 크기를 맞춘다.
        if (_ring != null)
        {
            SetDiameter(_ring, radius * 2f / _ringVisualRatio);
            float fade = 1f - Mathf.InverseLerp(_fadeStartRadius, _maxRadius, radius);
            _ring.color = WithAlpha(_ringColor, _ringColor.a * fade);
        }

        // 안쪽 빛 : 고리 크기를 따라가며 시간이 지나면 옅어진다.
        if (_fill != null)
        {
            SetDiameter(_fill, radius * 2f);
            float fade = 1f - Mathf.Clamp01(_elapsed / _fillDuration);
            _fill.color = WithAlpha(_fillColor, _fillColor.a * fade * fade);
        }

        // 시작 빛 : 시작 위치에서 잠깐 번쩍이며 살짝 커진다.
        if (_flash != null)
        {
            float t = Mathf.Clamp01(_elapsed / _flashDuration);
            SetDiameter(_flash, _flashRadius * 2f * (1f + 0.5f * t));
            _flash.color = WithAlpha(_flashColor, _flashColor.a * (1f - t) * (1f - t));
        }
    }

    private void SetVisible(bool isVisible)
    {
        if (_ring != null) _ring.enabled = isVisible;
        if (_fill != null) _fill.enabled = isVisible;
        if (_flash != null) _flash.enabled = isVisible;
    }

    /// <summary>스프라이트의 원래 크기를 기준으로 원하는 지름(유닛)이 되도록 크기를 바꾼다.</summary>
    private static void SetDiameter(SpriteRenderer renderer, float diameter)
    {
        if (renderer.sprite == null)
        {
            return;
        }

        float spriteSize = Mathf.Max(0.0001f, renderer.sprite.bounds.size.x);
        float scale = diameter / spriteSize;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
