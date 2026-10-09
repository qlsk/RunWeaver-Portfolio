using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면 연출 순서.
/// - 등장 : 검은 화면에서 배경이 드러나고, 잠시 뒤 메뉴가 차례로 떠오른다. (로고 빛은 TitleAmbience가 맞춘다)
/// - 시작(새 게임) : 고른 글자가 빛나고, 석판 룬이 강하게 빛나며 빛 알갱이가 룬으로 빨려 들어간다.
///   룬에서 빛이 퍼져 화면을 덮고, 살짝 당겨진 뒤 암전한다. 끝나면 받은 콜백을 부른다.
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
    [Tooltip("석판 룬 위치에서 퍼지는 빛 (둥근 이미지)")]
    [SerializeField] private RectTransform _flash;

    [Header("등장")]
    [SerializeField, Min(0.01f)] private float _fadeInDuration = 1.4f;
    [SerializeField, Min(0f)] private float _menuDelay = 2.5f;

    [Header("시작")]
    [SerializeField, Min(0.1f)] private float _flareRise = 0.6f;
    [SerializeField] private float _flareAmount = 1.5f;
    [SerializeField] private float _startZoom = 0.05f;
    [SerializeField, Min(0.1f)] private float _flashSpreadDelay = 0.35f;
    [SerializeField, Min(0.1f)] private float _flashSpreadDuration = 1.6f;
    [Tooltip("빛이 가장 클 때 반지름 (픽셀)")]
    [SerializeField] private float _flashMaxRadius = 1700f;
    [SerializeField, Min(0f)] private float _blackoutDelay = 2f;
    [SerializeField, Min(0.01f)] private float _blackoutDuration = 0.9f;

    private bool _isStarting;
    private Graphic _flashGraphic;

    private void Awake()
    {
        _flashGraphic = _flash != null ? _flash.GetComponent<Graphic>() : null;
        SetAlpha(_flashGraphic, 0f);
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
        float baseRadius = _flash != null ? Mathf.Max(1f, _flash.rect.width * 0.5f) : 1f;

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

            if (_flash != null)
            {
                float radius = press < _flashSpreadDelay
                    ? 40f * press / _flashSpreadDelay
                    : 40f + (_flashMaxRadius - 40f) * EaseOut((press - _flashSpreadDelay) / _flashSpreadDuration);
                float scale = radius / baseRadius;
                _flash.localScale = new Vector3(scale, scale, 1f);
                SetAlpha(_flashGraphic, Mathf.Clamp01(press / 1.2f));
            }

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
