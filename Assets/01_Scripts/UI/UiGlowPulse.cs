using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class UiGlowPulse : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float _cycleDurationSeconds = 2.4f;
    [SerializeField, Range(0f, 1f)] private float _minAlphaRatio = 0.3f;
    [SerializeField] private Vector2 _scaleRange = new(1f, 1.03f);

    private Image _image;
    private float _baseAlpha;
    private float _elapsedSeconds;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _baseAlpha = _image.color.a;
    }

    private void OnEnable()
    {
        _elapsedSeconds = 0f;

        ApplyPulse();
    }

    private void Update()
    {
        _elapsedSeconds += Time.unscaledDeltaTime;

        if (_elapsedSeconds >= _cycleDurationSeconds)
            _elapsedSeconds %= _cycleDurationSeconds;

        ApplyPulse();
    }

    private void ApplyPulse()
    {
        float currentTime = _elapsedSeconds / _cycleDurationSeconds;

        float pulseRatio = 0.5f * (1f - Mathf.Cos(currentTime * Mathf.PI * 2f));
        float alphaRatio = Mathf.Lerp(_minAlphaRatio, 1f, pulseRatio);
        float scaleRatio = Mathf.Lerp(_scaleRange.x, _scaleRange.y, pulseRatio);

        Color color = _image.color;
        color.a = _baseAlpha * alphaRatio;
        _image.color = color;
        _image.transform.localScale = Vector3.one * scaleRatio;
    }
}