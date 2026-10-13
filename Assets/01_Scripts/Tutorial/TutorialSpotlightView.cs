using System;
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

    [Tooltip("스포트라이트를 놓는 전체 화면 RectTransform")]
    [SerializeField] private RectTransform _root;
    [SerializeField] private Canvas _canvas;
    [Tooltip("단계 SO의 대상 id와 실제 UI를 잇는 목록")]
    [SerializeField] private Target[] _targets;

    private GameObject _current;

    public void Show(TutorialStepSO step)
    {
        Hide();

        if (step.OverlayPrefab == null)
        {
            Debug.LogError("[TutorialSpotlightView] 스포트라이트 프리팹이 지정되지 않았습니다.");
            return;
        }

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

        // Destroy는 프레임 끝에 실행되므로, 같은 프레임에 새 스포트라이트가 생겨도 겹치지 않게 먼저 끈다.
        _current.SetActive(false);
        Destroy(_current);
        _current = null;
    }

    /// <summary>대상 id에 해당하는 UI들의 화면 영역을 합쳐 루트 기준 중심과 크기로 돌려준다.</summary>
    private bool TryGetTargetBounds(System.Collections.Generic.IReadOnlyList<string> targetIds, out Vector2 center, out Vector2 size)
    {
        center = Vector2.zero;
        size = Vector2.zero;

        if (targetIds == null || _targets == null || _root == null)
            return false;

        Camera camera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        Vector3[] corners = new Vector3[4];
        bool found = false;

        foreach (string targetId in targetIds)
        {
            bool isTargetFound = false;
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

                isTargetFound = true;
            }

            // 일부 대상만 비추면 잘못된 안내가 되므로, 하나라도 없으면 실패로 본다.
            if (!isTargetFound)
            {
                Debug.LogError($"[TutorialSpotlightView] 스포트라이트 대상 '{targetId}'를 찾을 수 없습니다.");
                return false;
            }

            found = true;
        }

        if (!found)
            return false;

        center = (min + max) * 0.5f;
        size = max - min;
        return true;
    }
}
