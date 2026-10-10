using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면 연출 순서.
/// - 등장 : 검은 화면에서 배경이 드러나고, 잠시 뒤 메뉴가 차례로 떠오른다. (로고 빛은 TitleAmbience가 맞춘다)
/// - 시작(새 게임) : 고른 글자가 빛나고, 석판 룬이 빛나며 빛 알갱이가 룬으로 빨려 들어간다.
///   룬에서 푸른 빛이 점화하듯 일렁이며 번지고(코어 + 석판 따라 세로 빛줄기 + 넓은 후광),
///   화면 전체에 옅은 푸른 기운이 차오른 뒤 암전한다. 끝나면 받은 콜백을 부른다.
/// - 빛 이미지는 스크린 블렌드(UI/ScreenGlow)로 배경을 밝힌다. 알파가 빛의 세기다.
/// 실제 시간으로 움직인다.
/// </summary>
[DisallowMultipleComponent]
public class TitleSequence : MonoBehaviour
{
    [SerializeField] private TitleAmbience _ambience;
    [SerializeField] private TitleMenuView _menu;
    [SerializeField] private TitleMotes _motes;

    [Header("화면 덮개")]
    [Tooltip("화면 전체를 덮는 검은 이미지")]
    [SerializeField] private Graphic _fade;
    [Tooltip("석판 룬 위치에서 넓게 번지는 후광 (가우시안 이미지)")]
    [SerializeField] private RectTransform _flash;
    [Tooltip("룬 중심의 작은 빛 (가우시안 이미지)")]
    [SerializeField] private RectTransform _flashCore;
    [Tooltip("석판을 따라 위아래로 퍼지는 빛줄기 (가우시안 이미지를 세로로 늘림)")]
    [SerializeField] private RectTransform _flashStreak;
    [Tooltip("화면 전체에 차오르는 옅은 빛")]
    [SerializeField] private Graphic _flashWash;

    [Header("등장")]
    [SerializeField, Min(0.01f)] private float _fadeInDuration = 1.4f;
    [SerializeField, Min(0f)] private float _menuDelay = 2.5f;

    [Header("시작")]
    [SerializeField, Min(0.1f)] private float _flareRise = 0.6f;
    [SerializeField] private float _flareAmount = 1.5f;
    [SerializeField] private float _startZoom = 0.05f;

    [Header("시작 빛 세기 (0~1)")]
    [SerializeField, Range(0f, 1f)] private float _haloIntensity = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _coreIntensity = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _streakIntensity = 0.45f;
    [Tooltip("화면 전체에 고르게 깔리는 빛. 0이면 끈다 (기본은 꺼서 중심에서 멀수록 빛이 닿지 않게 한다)")]
    [SerializeField, Range(0f, 1f)] private float _washIntensity = 0f;
    [SerializeField, Min(0f)] private float _blackoutDelay = 2f;
    [SerializeField, Min(0.01f)] private float _blackoutDuration = 0.9f;

    // 가우시안 이미지는 반지름의 1/3.2가 표준편차다. (가장자리에서 알파 0)
    private const float GaussRadiusPerSigma = 3.2f;

    private bool _isStarting;
    private Graphic _flashGraphic;
    private Graphic _coreGraphic;
    private Graphic _streakGraphic;

    private void Awake()
    {
        _flashGraphic = _flash != null ? _flash.GetComponent<Graphic>() : null;
        _coreGraphic = _flashCore != null ? _flashCore.GetComponent<Graphic>() : null;
        _streakGraphic = _flashStreak != null ? _flashStreak.GetComponent<Graphic>() : null;
        SetAlpha(_flashGraphic, 0f);
        SetAlpha(_coreGraphic, 0f);
        SetAlpha(_streakGraphic, 0f);
        SetAlpha(_flashWash, 0f);
        SetAlpha(_fade, 1f);
    }

    private void Start()
    {
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        float start = Time.unscaledTime;
        bool menuShown = false;
        while (true)
        {
            float t = Time.unscaledTime - start;
            if (!_isStarting)
            {
                SetAlpha(_fade, 1f - EaseOut(t / _fadeInDuration));
            }

            if (!menuShown && t >= _menuDelay)
            {
                menuShown = true;
                if (_menu != null)
                {
                    _menu.PlayAppear();
                }
            }

            if (menuShown && t >= _fadeInDuration)
            {
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>시작 연출을 하고 끝나면 onFinished를 부른다.</summary>
    public void PlayStart(int itemIndex, Action onFinished)
    {
        if (_isStarting)
        {
            return;
        }

        _isStarting = true;
        StartCoroutine(PlayStartRoutine(itemIndex, onFinished));
    }

    private IEnumerator PlayStartRoutine(int itemIndex, Action onFinished)
    {
        float start = Time.unscaledTime;
        float end = _blackoutDelay + _blackoutDuration;

        while (true)
        {
            float press = Time.unscaledTime - start;
            if (_menu != null)
            {
                _menu.SetLocked(true);
                _menu.SetPress(itemIndex, press);
            }

            if (_ambience != null)
            {
                _ambience.Flare = _flareAmount * EaseOut(press / _flareRise);
                float z = EaseOut(press / 2f);
                _ambience.ExtraZoom = _startZoom * z * z;
            }

            if (_motes != null)
            {
                _motes.Attract = Mathf.Clamp01(press * 0.8f);
            }

            UpdateStartLight(press);

            float black = Mathf.Clamp01((press - _blackoutDelay) / _blackoutDuration);
            SetAlpha(_fade, black);

            if (press >= end)
            {
                break;
            }

            yield return null;
        }

        SetAlpha(_fade, 1f);
        onFinished?.Invoke();
    }

    /// <summary>룬에서 번지는 빛. 크기는 표준편차(픽셀), 세기는 알파로 정한다.</summary>
    private void UpdateStartLight(float press)
    {
        float ignite = SmoothStep(0f, 0.55f, press);
        // 점화하는 듯한 약한 일렁임
        float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(press * 14f, 0.37f);

        float coreSigma = 36f + 32f * EaseOut(press / 1.2f);
        SetGauss(_flashCore, coreSigma, coreSigma);
        SetAlpha(_coreGraphic, _coreIntensity * (0.56f + 0.44f * ignite) * flicker * ignite);

        SetGauss(_flashStreak, 32f + 20f * ignite, 240f + 400f * EaseOut(press / 1.4f));
        SetAlpha(_streakGraphic, _streakIntensity * ignite);

        float spread = EaseOut((press - 0.2f) / 1.8f);
        SetGauss(_flash, 160f + 480f * spread, 160f + 400f * spread);
        SetAlpha(_flashGraphic, _haloIntensity * SmoothStep(0.15f, 1.2f, press));

        SetAlpha(_flashWash, _washIntensity * SmoothStep(0.8f, 2.2f, press));
    }

    private static void SetGauss(RectTransform target, float sigmaX, float sigmaY)
    {
        if (target == null)
        {
            return;
        }

        float halfWidth = Mathf.Max(1f, target.rect.width * 0.5f);
        float halfHeight = Mathf.Max(1f, target.rect.height * 0.5f);
        target.localScale = new Vector3(sigmaX * GaussRadiusPerSigma / halfWidth,
            sigmaY * GaussRadiusPerSigma / halfHeight, 1f);
    }

    private static float SmoothStep(float from, float to, float x)
    {
        float t = Mathf.Clamp01((x - from) / (to - from));
        return t * t * (3f - 2f * t);
    }

    private static float EaseOut(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - (1f - x) * (1f - x) * (1f - x);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
        graphic.raycastTarget = false;
    }
}
