using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화면 루트의 바로 아래 자식들을 "아래에서 살짝 떠오르며 나타나기"로 함께 움직인다.
/// - 화면 전체를 덮는 자식(앵커 0~1, 어둡게 깔기 등)은 투명도만 바꾼다.
/// - 나머지 자식은 투명도와 함께 아래(-rise)에서 제자리로 올라온다.
/// 각 자식에 CanvasGroup이 없으면 붙인다. 원래 위치는 처음 만들 때 기억한다.
/// </summary>
public sealed class UiRiseParts
{
    private sealed class Part
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Vector2 BasePosition;
        public bool IsFullScreen;
    }

    private readonly List<Part> _parts = new();
    private readonly float _rise;

    public UiRiseParts(Transform root, float rise)
    {
        _rise = rise;
        foreach (Transform child in root)
        {
            if (child is not RectTransform rect)
            {
                continue;
            }

            CanvasGroup group = rect.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = rect.gameObject.AddComponent<CanvasGroup>();
            }

            _parts.Add(new Part
            {
                Rect = rect,
                Group = group,
                BasePosition = rect.anchoredPosition,
                IsFullScreen = rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one,
            });
        }
    }

    /// <summary>t = 0이면 숨김(아래), 1이면 제자리. t는 이미 이징된 값.</summary>
    public void Apply(float t)
    {
        foreach (Part part in _parts)
        {
            if (part.Rect == null)
            {
                continue;
            }

            part.Group.alpha = t;
            if (!part.IsFullScreen)
            {
                part.Rect.anchoredPosition = part.BasePosition + new Vector2(0f, -_rise * (1f - t));
            }
        }
    }

    /// <summary>화면 전체를 덮는 자식을 뺀 나머지만 투명도를 바꾼다. (위치는 제자리)</summary>
    public void FadeContent(float alpha, Transform except = null)
    {
        foreach (Part part in _parts)
        {
            if (part.Rect == null || part.IsFullScreen || part.Rect == except)
            {
                continue;
            }

            part.Group.alpha = alpha;
            part.Rect.anchoredPosition = part.BasePosition;
        }
    }

    public void Restore()
    {
        Apply(1f);
    }

    public static float EaseOut(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - (1f - x) * (1f - x) * (1f - x);
    }
}
