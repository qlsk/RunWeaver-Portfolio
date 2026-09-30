using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 페이드 인 → 유지 → (선택) 페이드 아웃 순서로 패널을 보여준다.
/// 미션 성공/실패 패널처럼 한 번 띄우고 사라지는 연출에 사용한다.
/// 게임이 일시정지(timeScale 0)여도 연출이 진행되도록 unscaled 시간을 사용한다.
/// </summary>
[DisallowMultipleComponent]
public class FadePanelView : MonoBehaviour
{
    private const float NoFadeOut = 0f;

    [Header("페이드 인")]
    [SerializeField, Range(0f, 1f)] private float _startAlpha;
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.15f;

    [Header("크기 강조 (1이면 사용 안 함)")]
    [SerializeField, Min(0.01f)] private float _startScale = 1f;

    [Header("유지 / 페이드 아웃")]
    [SerializeField, Min(0f)] private float _holdDuration = 3f;
    [Tooltip("0이면 페이드 아웃 없이 표시된 상태로 남는다.")]
    [SerializeField, Min(0f)] private float _fadeOutDuration = 0.5f;

    private CanvasGroup _canvasGroup;
    private Coroutine _playRoutine;
    private Vector3 _originalScale = Vector3.one;
    private bool _isInitialized;

    /// <summary>
    /// 유지 시간이 끝났을 때 발생한다. (페이드 아웃이 있으면 페이드 아웃이 끝난 뒤)
    /// </summary>
    public event Action Finished;

    public bool IsPlaying => _playRoutine != null;

    public void Play()
    {
        Initialize();

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
        }

        gameObject.SetActive(true);
        _playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Hide()
    {
        Initialize();

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        transform.localScale = _originalScale;
        gameObject.SetActive(false);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _originalScale = transform.localScale;

        if (!TryGetComponent(out _canvasGroup))
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 결과 패널이 떠 있어도 뒤쪽 UI(룬북 등) 클릭을 막지 않는다.
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator PlayRoutine()
    {
        yield return Animate(_fadeInDuration, _startAlpha, 1f, _startScale, 1f);

        yield return new WaitForSecondsRealtime(_holdDuration);

        if (_fadeOutDuration > NoFadeOut)
        {
            yield return Animate(_fadeOutDuration, 1f, 0f, 1f, 1f);
            gameObject.SetActive(false);
        }

        _playRoutine = null;
        Finished?.Invoke();
    }

    private IEnumerator Animate(float duration, float fromAlpha, float toAlpha, float fromScale, float toScale)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // 끝으로 갈수록 느려지는 곡선으로 빠르게 등장하는 느낌을 준다.
            float eased = 1f - (1f - t) * (1f - t);
            Apply(Mathf.Lerp(fromAlpha, toAlpha, eased), Mathf.Lerp(fromScale, toScale, eased));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Apply(toAlpha, toScale);
    }

    private void Apply(float alpha, float scale)
    {
        _canvasGroup.alpha = alpha;
        transform.localScale = _originalScale * scale;
    }
}
