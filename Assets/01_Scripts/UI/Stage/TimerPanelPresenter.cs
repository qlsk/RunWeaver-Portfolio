using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 스테이지 남은 제한 시간을 MM:SS로 표시한다. (기획서 8-3)
/// 경고(색, 강조, 소리)는 상태 소스가 허용할 때만 켠다. (정화 카운트다운, 생존 미션에서는 끈다)
/// - 미션 중 : 타이머 위에 "제한 시간" 라벨을 띄운다. (시작 카운트다운의 "임무 시작까지" 라벨과 같은 양식)
/// - 30초 이하 : 주황색. 30초가 되는 순간 글자가 한 번 크게 튀고 주의 소리를 낸다.
/// - 10초 이하 : 빨간색. 1초마다 글자가 커졌다 돌아오고 째깍 소리를 낸다.
/// - 시작 카운트다운 : 카운트다운 색. 마지막 몇 초만 1초마다 강조한다. (소리 없음)
/// - 제한 시간 없음 : 별도 문구
/// 목표 이름·설명·달성도는 씬에 연결된 텍스트에 표시한다. (크기, 폰트, 위치는 씬에서 조절)
/// </summary>
[DisallowMultipleComponent]
public class TimerPanelPresenter : MonoBehaviour
{
    private const int SecondsPerMinute = 60;

    [SerializeField] private StageStatusSource _source;
    [SerializeField] private TMP_Text _timerText;

    [Header("주의 (경고 전 단계)")]
    [Tooltip("이 시간(초) 이하부터 주의 색으로 바꾸고, 이 시간이 되는 순간 한 번 강조한다")]
    [SerializeField, Min(0)] private int _cautionSeconds = 30;
    [SerializeField] private Color _cautionColor = new(1f, 0.72f, 0.25f, 1f);
    [SerializeField, Min(1f)] private float _cautionScale = 1.5f;
    [SerializeField, Min(0.01f)] private float _cautionDuration = 0.5f;

    [Header("경고")]
    [SerializeField, Min(0)] private int _warningSeconds = 10;
    [SerializeField] private Color _warningColor = new(0.9f, 0.15f, 0.1f, 1f);
    [Tooltip("미션 중 이 시간(초) 이하에서 1초마다 글자를 강조한다")]
    [SerializeField, Min(0)] private int _emphasisSeconds = 10;
    [SerializeField, Min(1f)] private float _emphasisScale = 1.3f;
    [SerializeField, Min(0.01f)] private float _emphasisDuration = 0.3f;

    [Header("소리 (비워두면 소리를 내지 않는다)")]
    [Tooltip("주의 시간(30초)이 되는 순간")]
    [SerializeField] private AudioClip _cautionSound;
    [Tooltip("경고 구간에서 1초마다")]
    [SerializeField] private AudioClip _warningTickSound;
    [SerializeField, Range(0f, 1f)] private float _soundVolume = 0.8f;

    [Header("제한 시간 라벨")]
    [Tooltip("시간이 다 되면 실패하는 미션 중에 타이머 위에 보여줄 라벨. 시작 카운트다운 라벨과 같은 자리에 꺼진 상태로 둔다")]
    [SerializeField] private GameObject _timeLimitLabel;

    [Header("제한 시간 없음")]
    [SerializeField] private string _noTimeLimitText = "--:--";

    [Header("목표")]
    [Tooltip("목표 이름")]
    [SerializeField] private TMP_Text _objectiveNameText;
    [Tooltip("임무 안내 설명")]
    [SerializeField] private TMP_Text _objectiveDescriptionText;
    [Tooltip("목표 달성도 (예: 42%)")]
    [SerializeField] private TMP_Text _objectiveProgressText;
    [Tooltip("진행률 0%일 때의 텍스트 색상")]
    [SerializeField] private Color _objectiveStartColor = new(0.9f, 0.15f, 0.1f, 1f);
    [Tooltip("진행률 100%일 때의 텍스트 색상")]
    [SerializeField] private Color _objectiveCompleteColor = new(0.2f, 0.85f, 0.3f, 1f);

    [Header("Countdown")]
    [SerializeField] private Color _countdownColor = new(0.55f, 0.85f, 1f, 1f);
    [SerializeField] private string _countdownPrefix = "카운트다운";
    [SerializeField] private GameObject _countdownLabel;
    [Tooltip("시작 카운트다운 중 이 시간(초) 이하에서 1초마다 글자를 강조한다")]
    [SerializeField, Min(0)] private int _countdownEmphasisSeconds = 5;

    private Color _normalColor = Color.white;
    private Vector3 _normalScale = Vector3.one;
    private int _shownSeconds = int.MinValue;
    private bool _shownWarning;
    private Coroutine _emphasisRoutine;
    private bool _shownCountdown;
    private AudioSource _audioSource;

    private void Awake()
    {
        if (_timerText == null)
        {
            Debug.LogError("[TimerPanelPresenter] 타이머 텍스트가 할당되지 않았습니다.", this);
            return;
        }

        _normalColor = _timerText.color;
        _normalScale = _timerText.transform.localScale;

        if (_objectiveNameText == null || _objectiveProgressText == null)
        {
            Debug.LogWarning("[TimerPanelPresenter] 목표 텍스트가 할당되지 않아 타이머 아래 목표를 표시하지 않습니다.", this);
        }

        SetObjective(string.Empty, string.Empty);
    }

    private void OnEnable()
    {
        if (_source == null)
        {
            Debug.LogWarning("[TimerPanelPresenter] 스테이지 상태 소스가 할당되지 않았습니다.", this);
            return;
        }

        _source.RemainingTimeChanged += HandleRemainingTimeChanged;
        _source.ObjectiveChanged += SetObjective;
    }

    private void OnDisable()
    {
        if (_source != null)
        {
            _source.RemainingTimeChanged -= HandleRemainingTimeChanged;
            _source.ObjectiveChanged -= SetObjective;
        }

        StopEmphasis();
    }

    private void HandleRemainingTimeChanged(float remainingSeconds)
    {
        if (_timerText == null)
            return;

        if (remainingSeconds < 0f)
        {
            ShowNoTimeLimit();
            return;
        }

        // 0초가 되는 순간 00:00이 되도록 올림으로 표시한다.
        int seconds = Mathf.CeilToInt(remainingSeconds);
        bool isWarningEnabled = _source == null || _source.IsTimeWarningEnabled;
        bool isCountdown = _source != null && _source.IsStartCountdown;

        if (seconds == _shownSeconds && isWarningEnabled == _shownWarning && isCountdown == _shownCountdown)
            return;

        int previousSeconds = _shownSeconds;
        bool isNewSecond = seconds != _shownSeconds;
        _shownSeconds = seconds;
        _shownWarning = isWarningEnabled;
        _shownCountdown = isCountdown;

        string prefix = isCountdown ? _countdownPrefix : string.Empty;
        _timerText.text = $"{prefix}{seconds / SecondsPerMinute:00}:{seconds % SecondsPerMinute:00}";
        _timerText.color = GetTimerColor(seconds, isWarningEnabled, isCountdown);

        if (_countdownLabel != null)
            _countdownLabel.SetActive(isCountdown);

        SetTimeLimitLabelVisible(isWarningEnabled && !isCountdown);

        if (!isWarningEnabled)
        {
            StopEmphasis();
            return;
        }

        if (!isNewSecond || seconds <= 0)
            return;

        if (isCountdown)
        {
            if (seconds <= _countdownEmphasisSeconds)
                PlayEmphasis(_emphasisScale, _emphasisDuration);
        }
        else if (seconds <= _emphasisSeconds)
        {
            PlayEmphasis(_emphasisScale, _emphasisDuration);
            PlaySound(_warningTickSound);
        }
        else if (seconds <= _cautionSeconds && previousSeconds > _cautionSeconds)
        {
            // 주의 시간을 넘어가는 순간 한 번만 크게 강조한다.
            PlayEmphasis(_cautionScale, _cautionDuration);
            PlaySound(_cautionSound);
        }
    }

    private Color GetTimerColor(int seconds, bool isWarningEnabled, bool isCountdown)
    {
        if (isCountdown)
            return _countdownColor;

        if (!isWarningEnabled)
            return _normalColor;

        if (seconds <= _warningSeconds)
            return _warningColor;

        return seconds <= _cautionSeconds ? _cautionColor : _normalColor;
    }

    private void SetObjective(string objectiveName, string progressText)
    {
        if (_objectiveNameText != null)
        {
            _objectiveNameText.text = objectiveName;
        }

        if (_objectiveProgressText != null)
        {
            _objectiveProgressText.text = progressText;
            float progress = _source != null ? Mathf.Clamp01(_source.ObjectiveProgress) : 0f;
            _objectiveProgressText.color = Color.Lerp(_objectiveStartColor, _objectiveCompleteColor, progress);
        }

        if (_objectiveDescriptionText != null)
        {
            _objectiveDescriptionText.text = _source != null ? _source.ObjectiveDescription : string.Empty;
        }
    }

    private void ShowNoTimeLimit()
    {
        _shownSeconds = int.MinValue;
        StopEmphasis();
        _timerText.text = _noTimeLimitText;
        _timerText.color = _normalColor;
        SetTimeLimitLabelVisible(false);
    }

    private void SetTimeLimitLabelVisible(bool isVisible)
    {
        if (_timeLimitLabel != null && _timeLimitLabel.activeSelf != isVisible)
        {
            _timeLimitLabel.SetActive(isVisible);
        }
    }

    private void PlayEmphasis(float scale, float duration)
    {
        StopEmphasis();
        _emphasisRoutine = StartCoroutine(EmphasisRoutine(scale, duration));
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

    private IEnumerator EmphasisRoutine(float scale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            _timerText.transform.localScale = _normalScale * Mathf.Lerp(scale, 1f, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _timerText.transform.localScale = _normalScale;
        _emphasisRoutine = null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || !Application.isPlaying)
            return;

        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }

        _audioSource.volume = GameSettings.MasterVolume * GameSettings.SfxVolume;
        _audioSource.PlayOneShot(clip, _soundVolume);
    }
}