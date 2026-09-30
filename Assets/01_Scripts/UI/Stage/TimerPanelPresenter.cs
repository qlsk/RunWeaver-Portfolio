using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 스테이지 남은 제한 시간을 MM:SS로 표시한다. (기획서 8-3)
/// 10초 이하: 빨간색 / 5초 이하: 1초마다 글자가 커졌다 돌아오는 강조 / 제한 시간 없음: 별도 문구
/// </summary>
[DisallowMultipleComponent]
public class TimerPanelPresenter : MonoBehaviour
{
    private const int SecondsPerMinute = 60;

    [SerializeField] private StageStatusSource _source;
    [SerializeField] private TMP_Text _timerText;

    [Header("경고")]
    [SerializeField, Min(0)] private int _warningSeconds = 10;
    [SerializeField] private Color _warningColor = new(0.9f, 0.15f, 0.1f, 1f);
    [SerializeField, Min(0)] private int _emphasisSeconds = 5;
    [SerializeField, Min(1f)] private float _emphasisScale = 1.3f;
    [SerializeField, Min(0.01f)] private float _emphasisDuration = 0.3f;

    [Header("제한 시간 없음")]
    [SerializeField] private string _noTimeLimitText = "--:--";

    private Color _normalColor = Color.white;
    private Vector3 _normalScale = Vector3.one;
    private int _shownSeconds = int.MinValue;
    private Coroutine _emphasisRoutine;

    private void Awake()
    {
        if (_timerText == null)
        {
            Debug.LogError("[TimerPanelPresenter] 타이머 텍스트가 할당되지 않았습니다.", this);
            return;
        }

        _normalColor = _timerText.color;
        _normalScale = _timerText.transform.localScale;
    }

    private void OnEnable()
    {
        if (_source == null)
        {
            Debug.LogWarning("[TimerPanelPresenter] 스테이지 상태 소스가 할당되지 않았습니다.", this);
            return;
        }

        _source.RemainingTimeChanged += HandleRemainingTimeChanged;
    }

    private void OnDisable()
    {
        if (_source != null)
        {
            _source.RemainingTimeChanged -= HandleRemainingTimeChanged;
        }

        StopEmphasis();
    }

    private void HandleRemainingTimeChanged(float remainingSeconds)
    {
        if (_timerText == null)
        {
            return;
        }

        if (remainingSeconds < 0f)
        {
            ShowNoTimeLimit();
            return;
        }

        // 0초가 되는 순간 00:00이 되도록 올림으로 표시한다.
        int seconds = Mathf.CeilToInt(remainingSeconds);
        if (seconds == _shownSeconds)
        {
            return;
        }

        _shownSeconds = seconds;
        _timerText.text = $"{seconds / SecondsPerMinute:00}:{seconds % SecondsPerMinute:00}";
        _timerText.color = seconds <= _warningSeconds ? _warningColor : _normalColor;

        if (seconds <= _emphasisSeconds && seconds > 0)
        {
            PlayEmphasis();
        }
    }

    private void ShowNoTimeLimit()
    {
        _shownSeconds = int.MinValue;
        StopEmphasis();
        _timerText.text = _noTimeLimitText;
        _timerText.color = _normalColor;
    }

    private void PlayEmphasis()
    {
        StopEmphasis();
        _emphasisRoutine = StartCoroutine(EmphasisRoutine());
    }

    private void StopEmphasis()
    {
        if (_emphasisRoutine != null)
        {
            StopCoroutine(_emphasisRoutine);
            _emphasisRoutine = null;
        }

        if (_timerText != null)
        {
            _timerText.transform.localScale = _normalScale;
        }
    }

    private IEnumerator EmphasisRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _emphasisDuration)
        {
            float t = elapsed / _emphasisDuration;
            _timerText.transform.localScale = _normalScale * Mathf.Lerp(_emphasisScale, 1f, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _timerText.transform.localScale = _normalScale;
        _emphasisRoutine = null;
    }
}
