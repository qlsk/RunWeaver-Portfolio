using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TutorialSpotlightView : MonoBehaviour
{
    private const string HoleName = "Hole";
    private const string LabelPath = "Hole/Callout/Label";
    private const float HolePadding = 24f;

    [Serializable]
    private struct Target
    {
        public string Id;
        public RectTransform Rect;
    }

    [SerializeField] private RectTransform _root;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Target[] _targets;

    private GameObject _current;

    public void Show(TutorialStepSO step)
    {
        Hide();

        if (!TryGetTargetBounds(step.SpotlightTargetIds, out Vector2 center, out Vector2 size))
        {
            Debug.LogError("[TutorialSpotlightView] 스포트라이트 대상을 찾을 수 없습니다.");
            return;
        }

        _current = Instantiate(step.OverlayPrefab, _root);

        if (_current.transform.Find(HoleName) is RectTransform hole)
        {
            hole.anchoredPosition = center;
            hole.sizeDelta = size + Vector2.one * (HolePadding * 2f);
        }

        Transform label = _current.transform.Find(LabelPath);
        if (label != null && label.TryGetComponent(out TMP_Text labelText))
            labelText.text = step.OverlayText;
    }

    public void Hide()
    {
        if (_current == null)
            return;

        _current.SetActive(false);
        Destroy(_current);
        _current = null;
    }

    private bool TryGetTargetBounds(IReadOnlyList<string> targetIds, out Vector2 center, out Vector2 size)
    {
        center = Vector2.zero;
        size = Vector2.zero;

        if (targetIds == null || targetIds.Count == 0 || _targets == null || _root == null)
            return false;

        Camera camera = _canvas.GetUiCamera();
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);

        if (targetIds.Any(targetId => !TryAddTargetBounds(targetId, camera, ref min, ref max)))
            return false;

        center = (min + max) * 0.5f;
        size = max - min;
        return true;
    }

    private bool TryAddTargetBounds(string targetId, Camera camera, ref Vector2 min, ref Vector2 max)
    {
        var corners = new Vector3[4];
        var isFound = false;

        foreach (Target target in _targets)
        {
            if (target.Id != targetId || target.Rect == null)
                continue;

            target.Rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, camera, out Vector2 local);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            isFound = true;
        }

        return isFound;
    }
}