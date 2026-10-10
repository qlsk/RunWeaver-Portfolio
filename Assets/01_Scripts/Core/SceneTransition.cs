using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬을 옮길 때 화면을 검게 덮었다가 넘어가는 전환. (결과창의 타이틀로 돌아가기, 다음 개체 투입 등)
/// - 실제 시간으로 움직여서 게임이 멈춘 상태(결과창)에서도 동작한다.
/// - 검게 덮인 뒤 load를 부르고, 새 씬이 열리면 reveal이 켜져 있을 때만 검은 화면을 걷어낸다.
///   타이틀은 스스로 검은 화면에서 밝아지므로 reveal 없이 바로 지운다.
/// - 씬에 놓을 필요 없이 처음 부를 때 스스로 만들고, 전환이 끝나면 스스로 사라진다.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private const int SortingOrder = 1000;

    private static SceneTransition s_instance;

    private Image _black;
    private bool _sceneLoaded;

    /// <summary>전환이 진행 중인지 (중복 요청을 막는다)</summary>
    public static bool IsRunning => s_instance != null;

    /// <param name="load">화면이 다 덮인 뒤 부른다. 여기서 씬을 옮긴다.</param>
    /// <param name="delay">덮기 시작하기 전 기다리는 시간 (버튼 반짝임 등)</param>
    /// <param name="coverDuration">검게 덮이는 시간</param>
    /// <param name="revealDuration">새 씬에서 검은 화면이 걷히는 시간. 0이면 바로 지운다.</param>
    public static bool FadeAndLoad(Action load, float delay, float coverDuration, float revealDuration)
    {
        if (s_instance != null || load == null)
        {
            return false;
        }

        var go = new GameObject("SceneTransition");
        DontDestroyOnLoad(go);
        s_instance = go.AddComponent<SceneTransition>();
        s_instance.Build();
        s_instance.StartCoroutine(s_instance.Run(load, delay, coverDuration, revealDuration));
        return true;
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var imageObject = new GameObject("Black", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)imageObject.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        _black = imageObject.GetComponent<Image>();
        _black.raycastTarget = true; // 전환 중에는 아래 UI를 누르지 못하게 막는다
        SetAlpha(0f);
    }

    private IEnumerator Run(Action load, float delay, float coverDuration, float revealDuration)
    {
        float wait = 0f;
        while (wait < delay)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return Fade(0f, 1f, coverDuration);

        SceneManager.sceneLoaded += HandleSceneLoaded;
        _sceneLoaded = false;
        load();

        while (!_sceneLoaded)
        {
            yield return null;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (revealDuration > 0f)
        {
            // 새 씬의 첫 프레임이 자리 잡을 때까지 한 프레임 더 가린다.
            yield return null;
            _black.raycastTarget = false;
            yield return Fade(1f, 0f, revealDuration);
        }

        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(from, to, t * t * (3f - 2f * t)));
            yield return null;
        }

        SetAlpha(to);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _sceneLoaded = true;
    }

    private void SetAlpha(float alpha)
    {
        _black.color = new Color(0f, 0f, 0f, alpha);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (s_instance == this)
        {
            s_instance = null;
        }
    }
}
