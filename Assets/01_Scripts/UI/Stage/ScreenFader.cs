using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 전체를 검게 덮었다가 걷어내는 전환 연출. (층 이동 등)
/// - FadeThroughBlack : 검게 페이드 인 → onBlack 호출(이때 다음 층을 준비) → 잠깐 유지 → 페이드 아웃.
/// - 씬에 없으면 Instance에서 기본값으로 자동 생성한다.
///   빈 오브젝트에 이 컴포넌트를 붙여 두면 인스펙터에서 시간과 정렬 순서를 조절할 수 있다.
///   _blackImage를 비워 두면 전용 캔버스와 검은 이미지를 자식으로 만든다.
/// - 게임 시간(Time.deltaTime)으로 돌아가므로 일시정지 중에는 멈춘다.
/// - 걷히는 중에 게임이 멈추면(룬북, 일시정지) 그 화면들이 어둡게 가려지지 않도록 검은 화면을 잠시 숨긴다.
///   다시 움직이면 멈췄던 곳부터 이어서 걷힌다.
/// - 덮는 중(검게 되는 중, 유지 중)에는 IsCovering이 true다. 이때는 룬북과 일시정지를 열지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class ScreenFader : MonoBehaviour
{
    [Tooltip("화면 전체를 덮는 검은 이미지. 비워두면 자동으로 만든다.")]
    [SerializeField] private Image _blackImage;
    [Tooltip("자동으로 만드는 캔버스의 정렬 순서. HUD보다 위에 오도록 크게 둔다.")]
    [SerializeField] private int _sortingOrder = 500;
    [Tooltip("검은 화면이 되기까지의 시간(초)")]
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.6f;
    [Tooltip("검은 화면을 유지하는 시간(초). 이 동안 다음 층이 준비된다.")]
    [SerializeField, Min(0f)] private float _holdDuration = 0.35f;
    [Tooltip("검은 화면이 걷히기까지의 시간(초)")]
    [SerializeField, Min(0f)] private float _fadeOutDuration = 0.9f;

    private static ScreenFader s_instance;

    private Coroutine _routine;
    private bool _isCovering;
    private bool _isBlack;

    /// <summary>전환 연출이 진행 중인지 (이때는 일시정지를 열지 않는다)</summary>
    public static bool IsFading => s_instance != null && s_instance._routine != null;

    /// <summary>화면을 덮는 중인지 (검게 되는 중 + 유지 중). 걷히는 중에는 false.</summary>
    public static bool IsCovering => IsFading && s_instance._isCovering;

    /// <summary>화면이 완전히 검게 덮여 있는지 (검게 된 뒤 유지하는 동안)</summary>
    public static bool IsBlack => IsFading && s_instance._isBlack;

    public static ScreenFader Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = FindAnyObjectByType<ScreenFader>(FindObjectsInactive.Include);
                if (s_instance == null)
                {
                    s_instance = new GameObject("ScreenFader").AddComponent<ScreenFader>();
                }
            }

            return s_instance;
        }
    }

    private void Awake()
    {
        if (s_instance == null)
        {
            s_instance = this;
        }
        else if (s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureBlackImage();
        SetAlpha(0f);
    }

    private void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    /// <summary>검게 덮은 뒤 onBlack을 부르고, 잠깐 유지했다가 걷어낸다.</summary>
    public void FadeThroughBlack(Action onBlack)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        EnsureBlackImage();

        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _routine = StartCoroutine(FadeThroughBlackRoutine(onBlack));
    }

    private IEnumerator FadeThroughBlackRoutine(Action onBlack)
    {
        _blackImage.raycastTarget = true;
        _isCovering = true;
        yield return FadeRoutine(_blackImage.color.a, 1f, _fadeInDuration, false);
        _isBlack = true;

        onBlack?.Invoke();

        float wait = 0f;
        while (wait < _holdDuration)
        {
            wait += Time.deltaTime;
            yield return null;
        }

        _isBlack = false;
        _isCovering = false;
        yield return FadeRoutine(1f, 0f, _fadeOutDuration, true);
        _blackImage.raycastTarget = false;
        _routine = null;
    }

    /// <param name="hideWhilePaused">게임이 멈춘 동안 검은 화면을 숨긴다 (걷히는 중에만)</param>
    private IEnumerator FadeRoutine(float from, float to, float duration, bool hideWhilePaused)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            bool isPaused = hideWhilePaused && Time.timeScale <= 0f;
            SetAlpha(isPaused ? 0f : alpha);
            _blackImage.raycastTarget = !isPaused;
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        if (_blackImage == null)
        {
            return;
        }

        Color color = _blackImage.color;
        color.a = alpha;
        _blackImage.color = color;
        _blackImage.enabled = alpha > 0f;
    }

    private void EnsureBlackImage()
    {
        if (_blackImage != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("ScreenFaderCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = _sortingOrder;

        GameObject imageObject = new GameObject("Black", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _blackImage = imageObject.GetComponent<Image>();
        _blackImage.color = new Color(0f, 0f, 0f, 0f);
        _blackImage.raycastTarget = false;
    }
}
