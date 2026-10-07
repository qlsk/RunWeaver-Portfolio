using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 스테이지 남은 제한 시간을 MM:SS로 표시한다. (기획서 8-3)
/// 10초 이하: 빨간색 / 5초 이하: 1초마다 글자가 커졌다 돌아오는 강조 / 제한 시간 없음: 별도 문구
/// 타이머 아래에는 현재 목표 이름(Survive)과 달성도(42%)를 한 줄씩 표시한다. (미션 두루마리 대체)
/// 목표 텍스트를 지정하지 않으면 타이머 텍스트 모양을 따라 타이머 아래에 만든다.
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

    [Header("목표 (타이머 아래)")]
    [Tooltip("비워두면 타이머 텍스트 아래에 자동으로 만든다")]
    [SerializeField] private TMP_Text _objectiveNameText;
    [Tooltip("비워두면 타이머 텍스트 아래에 자동으로 만든다")]
    [SerializeField] private TMP_Text _objectiveProgressText;
    [Tooltip("자동으로 만들 때 타이머 글자 크기 대비 비율")]
    [SerializeField, Range(0.2f, 1f)] private float _objectiveFontScale = 0.55f;
    [Tooltip("자동으로 만들 때 줄 간격 (타이머 높이 대비 비율)")]
    [SerializeField, Range(0.2f, 2f)] private float _objectiveLineSpacing = 0.75f;

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

        if (_objectiveNameText == null)
        {
            _objectiveNameText = CreateObjectiveText("ObjectiveNameText", 1);
        }

        if (_objectiveProgressText == null)
        {
            _objectiveProgressText = CreateObjectiveText("ObjectiveProgressText", 2);
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

    private void SetObjective(string objectiveName, string progressText)
    {
        if (_objectiveNameText != null)
        {
            _objectiveNameText.text = objectiveName;
        }

        if (_objectiveProgressText != null)
        {
            _objectiveProgressText.text = progressText;
        }
    }

    /// <summary>타이머 텍스트의 글꼴, 색, 정렬을 따라 타이머 아래 line번째 줄에 텍스트를 만든다.</summary>
    private TMP_Text CreateObjectiveText(string objectName, int line)
    {
        RectTransform timerRect = _timerText.rectTransform;
        GameObject textObject = new(objectName, typeof(RectTransform));
        textObject.layer = _timerText.gameObject.layer;

        RectTransform rect = (RectTransform)textObject.transform;
        rect.SetParent(timerRect.parent, false);
        rect.anchorMin = timerRect.anchorMin;
        rect.anchorMax = timerRect.anchorMax;
        rect.pivot = timerRect.pivot;
        rect.sizeDelta = new Vector2(timerRect.sizeDelta.x, timerRect.sizeDelta.y * _objectiveLineSpacing);

        float lineHeight = Mathf.Max(1f, timerRect.rect.height) * _objectiveLineSpacing;
        rect.anchoredPosition = timerRect.anchoredPosition - new Vector2(0f, lineHeight * line);

        TMP_Text text = _timerText is TextMeshProUGUI
            ? (TMP_Text)textObject.AddComponent<TextMeshProUGUI>()
            : textObject.AddComponent<TextMeshPro>();
        text.font = _timerText.font;
        text.fontSharedMaterial = _timerText.fontSharedMaterial;
        text.fontSize = _timerText.fontSize * _objectiveFontScale;
        text.enableAutoSizing = false;
        text.color = _normalColor;
        text.alignment = _timerText.alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
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
