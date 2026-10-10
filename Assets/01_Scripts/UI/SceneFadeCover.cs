using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬을 옮길 때 화면을 검게 덮었다가, 다음 씬이 열리면 걷어내는 덮개. (결과창의 타이틀로 돌아가기, 다음 개체 투입)
/// - Cover : 검게 덮은 뒤(실제 시간) onCovered를 부른다. onCovered에서 씬을 옮긴다.
/// - 덮개는 씬이 바뀌어도 남아 있다가, 새 씬이 열리면 천천히 걷히고 스스로 사라진다.
///   타이틀은 원래 검은 화면에서 밝아지므로 이어서 자연스럽게 보인다.
/// - 일시정지(timeScale 0) 중에도 돌아가도록 실제 시간을 쓴다.
/// </summary>
public class SceneFadeCover : MonoBehaviour
{
    private const int SortingOrder = 1000;

    private Image _black;
    private float _revealDuration = 0.5f;
    private bool _waitingForScene;

    /// <summary>화면을 duration 동안 검게 덮고 onCovered를 부른다.</summary>
    public static SceneFadeCover Cover(float delay, float duration, float revealDuration, Action onCovered)
    {
        var go = new GameObject("SceneFadeCover", typeof(RectTransform), typeof(Canvas));
        DontDestroyOnLoad(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var imageObject = new GameObject("Black", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)imageObject.transform;
        rect.SetParent(go.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var cover = go.AddComponent<SceneFadeCover>();
        cover._black = imageObject.GetComponent<Image>();
        cover._black.color = new Color(0f, 0f, 0f, 0f);
        cover._black.raycastTarget = true; // 덮는 동안 다른 입력을 막는다
        cover._revealDuration = Mathf.Max(0.01f, revealDuration);
        cover.StartCoroutine(cover.CoverRoutine(delay, duration, onCovered));
        return cover;
    }

    private IEnumerator CoverRoutine(float delay, float duration, Action onCovered)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(t * t * (3f - 2f * t));
            yield return null;
        }

        SetAlpha(1f);
        _waitingForScene = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        onCovered?.Invoke();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (!_waitingForScene)
        {
            return;
        }

        _waitingForScene = false;
        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        // 새 씬의 첫 프레임이 그려진 뒤에 걷는다. (씬이 준비되는 순간이 비치지 않게)
        yield return null;
        _black.raycastTarget = false;

        float elapsed = 0f;
        while (elapsed < _revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _revealDuration);
            SetAlpha(1f - t * t * (3f - 2f * t));
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void SetAlpha(float alpha)
    {
        if (_black != null)
        {
            _black.color = new Color(0f, 0f, 0f, alpha);
        }
    }
}
