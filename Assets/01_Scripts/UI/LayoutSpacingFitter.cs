using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 세로 목록(VerticalLayoutGroup)의 항목이 늘어나도 칸 안에 들어가도록 항목 사이 간격을 줄인다.
/// (일시정지 메뉴 : 튜토리얼 중에는 튜토리얼 건너뛰기, 안내서 버튼이 더해진다)
/// 간격은 최대 간격을 넘지 않고, 최소 간격보다 줄지 않는다. 켜진 항목만 센다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(VerticalLayoutGroup))]
public class LayoutSpacingFitter : MonoBehaviour
{
    [Tooltip("항목이 적을 때 쓰는 간격 (원래 간격)")]
    [SerializeField, Min(0f)] private float _maxSpacing = 12f;
    [Tooltip("항목이 많아도 이보다 좁히지 않는다")]
    [SerializeField] private float _minSpacing = -4f;

    private VerticalLayoutGroup _group;
    private RectTransform _rect;
    private int _lastCount = -1;
    private float _lastHeight = -1f;

    private void Awake()
    {
        _group = GetComponent<VerticalLayoutGroup>();
        _rect = transform as RectTransform;
    }

    private void LateUpdate()
    {
        if (_group == null || _rect == null)
        {
            return;
        }

        int count = 0;
        float total = 0f;
        foreach (RectTransform child in _rect)
        {
            if (!child.gameObject.activeSelf)
            {
                continue;
            }

            LayoutElement element = child.GetComponent<LayoutElement>();
            if (element != null && element.ignoreLayout)
            {
                continue;
            }

            count++;
            total += child.rect.height;
        }

        float height = _rect.rect.height - _group.padding.vertical;
        if (count == _lastCount && Mathf.Approximately(height, _lastHeight))
        {
            return;
        }

        _lastCount = count;
        _lastHeight = height;
        float spacing = count > 1 ? (height - total) / (count - 1) : _maxSpacing;
        spacing = Mathf.Clamp(spacing, _minSpacing, _maxSpacing);
        if (!Mathf.Approximately(_group.spacing, spacing))
        {
            _group.spacing = spacing;
            LayoutRebuilder.MarkLayoutForRebuild(_rect);
        }
    }
}
