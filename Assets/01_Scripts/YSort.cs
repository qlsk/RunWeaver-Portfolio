using UnityEngine;
using UnityEngine.Rendering;

/// <summary>지면의 고정 기준점으로 스프라이트와 그룹의 앞뒤 순서를 정한다.</summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class YSort : MonoBehaviour
{
    [SerializeField] private float _sortingOffsetY;

    private SortingGroup _sortingGroup;
    private SpriteRenderer _spriteRenderer;

    private void OnEnable()
    {
        _sortingGroup = GetComponent<SortingGroup>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ApplySorting();
    }

    private void LateUpdate()
    {
        ApplySorting();
    }

    private void ApplySorting()
    {
        float groundY = transform.TransformPoint(new Vector3(0f, _sortingOffsetY, 0f)).y;
        int order = Mathf.Clamp(10000 - Mathf.RoundToInt(groundY * 100f), -32768, 32767);

        if (_sortingGroup != null)
            _sortingGroup.sortingOrder = order;
        else if (_spriteRenderer != null)
            _spriteRenderer.sortingOrder = order;
    }
}
