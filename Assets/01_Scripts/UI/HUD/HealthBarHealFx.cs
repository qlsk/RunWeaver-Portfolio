using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 체력 바의 회복 연출.
/// - 늘어날 만큼이 먼저 민트색으로 차는 것은 MMProgressBar의 증가 지연 바(DelayedBarIncreasing, IncreasingFill)가 맡는다.
///   빨간 바는 IncreasingDelay 뒤에 따라 올라간다.
/// - 이 컴포넌트는 체력이 늘어난 순간 바 전체를 한 번 밝히고, 차오른 끝에 빛을 두고, 늘어난 구간에서 빛 알갱이를 띄운다.
/// - 그림은 프리팹의 자식 이미지 (HealFlash, HealTip, HealMote 견본)를 쓴다.
/// 플레이어 체력(PlayerHealth)은 씬에서 찾아 매 프레임 비교한다. 게임 시간으로 흐른다.
/// </summary>
[DisallowMultipleComponent]
public class HealthBarHealFx : MonoBehaviour
{
    private const float SearchInterval = 1f;

    [Header("그림")]
    [Tooltip("체력 바 앞쪽 채움 (Fill). 끝 빛과 빛 알갱이는 이 영역 기준으로 놓는다")]
    [SerializeField] private RectTransform _fill;
    [Tooltip("바 전체가 한 번 밝아지는 빛")]
    [SerializeField] private Image _flash;
    [Tooltip("차오른 끝에 맺히는 빛 (Fill의 자식)")]
    [SerializeField] private Image _tip;
    [Tooltip("빛 알갱이 견본 (Fill의 자식, 꺼 둔다)")]
    [SerializeField] private Image _moteTemplate;

    [Header("시간")]
    [SerializeField, Min(0.01f)] private float _flashDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float _tipDuration = 0.8f;

    [Header("빛 알갱이")]
    [SerializeField, Min(0)] private int _moteCount = 10;
    [Tooltip("떠오르는 높이 (픽셀)")]
    [SerializeField] private Vector2 _moteRise = new(18f, 40f);
    [SerializeField] private Vector2 _moteLife = new(0.5f, 0.9f);
    [Tooltip("알갱이마다 늦게 나오는 정도 (최대)")]
    [SerializeField, Min(0f)] private float _moteStagger = 0.25f;

    [Tooltip("이보다 적게 오른 회복은 연출하지 않는다 (최대 체력 비율)")]
    [SerializeField, Range(0f, 0.2f)] private float _minRatioGain = 0.005f;

    private sealed class Mote
    {
        public Image Image;
        public Vector2 Start;
        public float Rise;
        public float Delay;
        public float Life;
        public float Age;
    }

    private readonly List<Mote> _motes = new();
    private readonly Stack<Image> _motePool = new();
    private PlayerHealth _health;
    private float _nextSearchTime;
    private float _lastRatio = -1f;
    private float _flashTime = -1f;
    private float _tipTime = -1f;
    private Color _flashColor = Color.white;
    private Color _tipColor = Color.white;
    private Color _moteColor = Color.white;

    private void Awake()
    {
        if (_flash != null)
        {
            _flashColor = _flash.color;
            _flash.gameObject.SetActive(false);
        }

        if (_tip != null)
        {
            _tipColor = _tip.color;
            _tip.gameObject.SetActive(false);
        }

        if (_moteTemplate != null)
        {
            _moteColor = _moteTemplate.color;
            _moteTemplate.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        FindHealth();
        if (_health != null && _health.MaxHealth > 0f)
        {
            float ratio = Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth);
            if (_lastRatio >= 0f && ratio - _lastRatio >= _minRatioGain && !_health.IsDead)
            {
                Play(_lastRatio, ratio);
            }

            _lastRatio = ratio;
        }

        float deltaTime = Time.deltaTime;
        UpdateFlash(deltaTime);
        UpdateTip(deltaTime);
        UpdateMotes(deltaTime);
    }

    private void FindHealth()
    {
        if (_health != null || Time.unscaledTime < _nextSearchTime)
        {
            return;
        }

        _nextSearchTime = Time.unscaledTime + SearchInterval;
        _health = FindAnyObjectByType<PlayerHealth>();
        _lastRatio = -1f;
    }

    private void Play(float from, float to)
    {
        _flashTime = 0f;
        if (_flash != null)
        {
            _flash.gameObject.SetActive(true);
        }

        _tipTime = 0f;
        if (_tip != null)
        {
            _tip.rectTransform.anchoredPosition = BarPoint(to, 0f);
            _tip.gameObject.SetActive(true);
        }

        if (_moteTemplate == null)
        {
            return;
        }

        for (int i = 0; i < _moteCount; i++)
        {
            Image image = _motePool.Count > 0
                ? _motePool.Pop()
                : Instantiate(_moteTemplate, _moteTemplate.transform.parent);
            image.gameObject.SetActive(false);
            _motes.Add(new Mote
            {
                Image = image,
                Start = BarPoint(Random.Range(from, to), Random.Range(-0.3f, 0.3f)),
                Rise = Random.Range(_moteRise.x, _moteRise.y),
                Delay = Random.Range(0f, _moteStagger),
                Life = Random.Range(_moteLife.x, _moteLife.y)
            });
        }
    }

    /// <summary>
    /// Fill 영역에서 가로 비율 x, 세로 비율 y(-0.5 ~ 0.5)인 점.
    /// 자식 이미지의 앵커가 Fill 가운데이므로 Fill 가운데 기준 좌표로 돌려준다.
    /// </summary>
    private Vector2 BarPoint(float x, float y)
    {
        if (_fill == null)
        {
            return Vector2.zero;
        }

        Rect rect = _fill.rect;
        return new Vector2(rect.width * (x - 0.5f), rect.height * y);
    }

    private void UpdateFlash(float deltaTime)
    {
        if (_flashTime < 0f || _flash == null)
        {
            return;
        }

        _flashTime += deltaTime;
        float t = _flashTime / _flashDuration;
        if (t >= 1f)
        {
            _flashTime = -1f;
            _flash.gameObject.SetActive(false);
            return;
        }

        Color color = _flashColor;
        color.a *= Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        _flash.color = color;
    }

    private void UpdateTip(float deltaTime)
    {
        if (_tipTime < 0f || _tip == null)
        {
            return;
        }

        _tipTime += deltaTime;
        float t = _tipTime / _tipDuration;
        if (t >= 1f)
        {
            _tipTime = -1f;
            _tip.gameObject.SetActive(false);
            return;
        }

        Color color = _tipColor;
        color.a *= Mathf.Clamp01(t / 0.1f) * (1f - t);
        _tip.color = color;
        _tip.rectTransform.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(t * Mathf.PI * 3f));
    }

    private void UpdateMotes(float deltaTime)
    {
        for (int i = _motes.Count - 1; i >= 0; i--)
        {
            Mote mote = _motes[i];
            if (mote.Image == null)
            {
                _motes.RemoveAt(i);
                continue;
            }

            mote.Age += deltaTime;
            float time = mote.Age - mote.Delay;
            if (time < 0f)
            {
                continue;
            }

            float t = time / mote.Life;
            if (t >= 1f)
            {
                mote.Image.gameObject.SetActive(false);
                _motePool.Push(mote.Image);
                _motes.RemoveAt(i);
                continue;
            }

            if (!mote.Image.gameObject.activeSelf)
            {
                mote.Image.gameObject.SetActive(true);
            }

            mote.Image.rectTransform.anchoredPosition = mote.Start + Vector2.up * (mote.Rise * (1f - (1f - t) * (1f - t)));
            Color color = _moteColor;
            color.a *= Mathf.Clamp01(t / 0.15f) * (1f - t);
            mote.Image.color = color;
        }
    }
}
