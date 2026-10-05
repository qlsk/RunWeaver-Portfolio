using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 일시정지 해제 전 3, 2, 1 카운트다운. 게임이 멈춘 상태(timeScale 0)에서 돌아가도록 실제 시간을 쓴다.
/// 숫자가 바뀔 때마다 살짝 커졌다가 원래 크기로 돌아온다.
/// </summary>
[DisallowMultipleComponent]
public class CountdownView : MonoBehaviour
{
    [SerializeField] private TMP_Text _numberText;
    [SerializeField, Min(1f)] private float _popScale = 1.35f;
    [SerializeField, Min(0.01f)] private float _popDuration = 0.25f;

    private Coroutine _routine;

    public bool IsPlaying => _routine != null;

    public void Play(int seconds, Action onFinished)
    {
        Stop();
        gameObject.SetActive(true);
        _routine = StartCoroutine(PlayRoutine(Mathf.Max(1, seconds), onFinished));
    }

    public void Stop()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator PlayRoutine(int seconds, Action onFinished)
    {
        for (int i = seconds; i > 0; i--)
        {
            if (_numberText != null)
            {
                _numberText.text = i.ToString();
            }

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_numberText != null)
                {
                    float t = Mathf.Clamp01(elapsed / _popDuration);
                    float scale = Mathf.Lerp(_popScale, 1f, 1f - (1f - t) * (1f - t));
                    _numberText.rectTransform.localScale = Vector3.one * scale;
                }

                yield return null;
            }
        }

        _routine = null;
        gameObject.SetActive(false);
        onFinished?.Invoke();
    }
}
