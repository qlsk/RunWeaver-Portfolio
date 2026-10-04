using TMPro;
using UnityEngine;

/// <summary>
/// 새 룬북(RuneBookV2)의 짧은 안내 문구 (예: All slots are full).
/// 잠깐 보여준 뒤 서서히 사라진다. 룬북이 열린 동안 게임이 멈추므로(timeScale 0) 실제 시간으로 잰다.
/// 바탕 너비는 문구 길이에 맞춘다.
/// </summary>
[DisallowMultipleComponent]
public class RuneBookToastView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _background;
    [SerializeField] private TMP_Text _text;
    [SerializeField] private float _horizontalPadding = 30f;
    [SerializeField] private float _showSeconds = 1.6f;
    [SerializeField] private float _fadeSeconds = 0.35f;

    private float _elapsed = float.MaxValue;

    private void Awake()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
    }

    private void OnDisable()
    {
        _elapsed = float.MaxValue;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        if (_canvasGroup == null || _elapsed == float.MaxValue)
        {
            return;
        }

        _elapsed += Time.unscaledDeltaTime;
        float fade = _elapsed - _showSeconds;
        _canvasGroup.alpha = fade <= 0f ? 1f : Mathf.Clamp01(1f - fade / Mathf.Max(0.01f, _fadeSeconds));
        if (_canvasGroup.alpha <= 0f)
        {
            _elapsed = float.MaxValue;
        }
    }

    public void Show(string message)
    {
        if (_text != null)
        {
            _text.text = message;
            if (_background != null)
            {
                float width = _text.GetPreferredValues(message).x + _horizontalPadding * 2f;
                _background.sizeDelta = new Vector2(width, _background.sizeDelta.y);
            }
        }

        _elapsed = 0f;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }
    }
}
