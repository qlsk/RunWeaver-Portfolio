using UnityEngine;

/// <summary>
/// 보상을 받은 뒤 캐릭터 발밑에 금빛 룬 문양과 고리가 한 번 퍼졌다 사라지는 연출.
/// 플레이어 프리팹 안에 두고, 보상 UI 연출(RewardSelectPresentation)이 끝날 때 Play를 부른다.
/// 그림은 자식 SpriteRenderer에 할당한 이미지를 쓴다. 바닥에 누운 모양은 _flatten(세로 배율)으로 만든다.
/// 게임 시간으로 흐른다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerRewardAcquireFx : MonoBehaviour
{
    [Header("그림")]
    [SerializeField] private SpriteRenderer _sigil;
    [SerializeField] private SpriteRenderer _ring;

    [Header("위치와 모양")]
    [Tooltip("플레이어 위치에서 발밑까지의 거리")]
    [SerializeField] private Vector2 _footOffset = new(0f, -0.6f);
    [Tooltip("바닥에 누운 정도 (세로 배율)")]
    [SerializeField, Range(0.1f, 1f)] private float _flatten = 0.38f;

    [Header("룬 문양")]
    [Tooltip("크기 (시작, 끝)")]
    [SerializeField] private Vector2 _sigilScale = new(0.55f, 0.9f);
    [SerializeField, Min(0.01f)] private float _sigilGrowDuration = 0.6f;
    [Tooltip("사라지기 시작하는 시간")]
    [SerializeField, Min(0f)] private float _sigilFadeDelay = 0.2f;
    [SerializeField, Min(0.01f)] private float _sigilFadeDuration = 0.8f;

    [Header("고리")]
    [Tooltip("크기 (시작, 끝)")]
    [SerializeField] private Vector2 _ringScale = new(0.5f, 1.8f);
    [SerializeField, Min(0.01f)] private float _ringDuration = 0.7f;

    private Color _sigilColor = Color.white;
    private Color _ringColor = Color.white;
    private float _age = -1f;

    private void Awake()
    {
        if (_sigil != null)
        {
            _sigilColor = _sigil.color;
        }

        if (_ring != null)
        {
            _ringColor = _ring.color;
        }

        Hide();
    }

    public void Play()
    {
        _age = 0f;
        Refresh();
    }

    private void Update()
    {
        if (_age < 0f)
        {
            return;
        }

        _age += Time.deltaTime;
        if (_age >= Mathf.Max(_sigilFadeDelay + _sigilFadeDuration, _ringDuration))
        {
            _age = -1f;
            Hide();
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        Vector3 foot = transform.parent != null
            ? transform.parent.position + (Vector3)_footOffset
            : transform.position + (Vector3)_footOffset;

        if (_sigil != null)
        {
            float grow = Mathf.Lerp(_sigilScale.x, _sigilScale.y, EaseOut(_age / _sigilGrowDuration));
            float alpha = 1f - SmoothStep((_age - _sigilFadeDelay) / _sigilFadeDuration);
            _sigil.enabled = alpha > 0f;
            _sigil.transform.position = foot;
            _sigil.transform.localScale = new Vector3(grow, grow * _flatten, 1f);
            _sigil.color = WithAlpha(_sigilColor, _sigilColor.a * alpha);
        }

        if (_ring != null)
        {
            float p = Mathf.Clamp01(_age / _ringDuration);
            float grow = Mathf.Lerp(_ringScale.x, _ringScale.y, EaseOut(p));
            _ring.enabled = p < 1f;
            _ring.transform.position = foot;
            _ring.transform.localScale = new Vector3(grow, grow * _flatten, 1f);
            _ring.color = WithAlpha(_ringColor, _ringColor.a * (1f - SmoothStep(p)));
        }
    }

    private void Hide()
    {
        if (_sigil != null)
        {
            _sigil.enabled = false;
        }

        if (_ring != null)
        {
            _ring.enabled = false;
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static float EaseOut(float value)
    {
        value = Mathf.Clamp01(value);
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }

    private static float SmoothStep(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}
