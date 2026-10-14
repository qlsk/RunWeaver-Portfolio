using Sirenix.OdinInspector;
using UnityEngine;

/// 원소 반응 영역의 중심에 반복 재생하는 회오리 스프라이트 하나를 생성한다.
public class ReactionAreaVisuals : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private GameObject _effectPrefab;
    [FoldoutGroup("Visual Settings"), SerializeField, Min(0.01f)] private float _referenceRadius = 4f;
    [Tooltip("바닥 위로 솟는 부분(기둥, 구름)의 기준 반지름. 0이면 바닥과 같은 비율로 키운다. " +
             "바닥 그림은 판정 범위에 정확히 맞추고, 솟는 부분은 이 값으로 따로 크기를 정해 지나치게 커지지 않게 한다.")]
    [FoldoutGroup("Visual Settings"), SerializeField, Min(0f)] private float _columnReferenceRadius;
    [Tooltip("솟는 부분으로 취급할 이펙트 자식 이름")]
    [FoldoutGroup("Visual Settings"), SerializeField] private string[] _columnPartNames = { "Column", "Cloud" };
    [Tooltip("끄면 이펙트 프리팹의 정렬 값을 그대로 쓴다. (적과 Y 정렬로 앞뒤를 섞고 싶을 때)")]
    [FoldoutGroup("Visual Settings"), SerializeField] private bool _overrideSorting = true;

    private GameObject _effect;

    /// 원본 회오리의 최대 반경을 기준으로 크기를 맞추고 장판과 함께 반복 재생한다.
    public void Initialize(float radius)
    {
        if (_effect != null)
        {
            _effect.SetActive(false);
            Destroy(_effect);
            _effect = null;
        }

        if (_effectPrefab == null || radius <= 0f)
            return;

        _effect = Instantiate(_effectPrefab, transform.position,
            _effectPrefab.transform.rotation, transform);
        float scale = radius / Mathf.Max(0.01f, _referenceRadius);
        _effect.transform.localScale *= scale;
        ScaleColumnParts(radius, scale);
        if (!_overrideSorting)
            return;

        // Field(-20) 위, 일반 오브젝트(-1 이상) 아래에 고정한다.
        foreach (SpriteRenderer renderer in _effect.GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = -10;
        }
    }

    /// 솟는 부분만 _columnReferenceRadius 기준 크기로 되돌린다.
    private void ScaleColumnParts(float radius, float groundScale)
    {
        if (_columnReferenceRadius <= 0f || _columnPartNames == null)
            return;

        float ratio = radius / _columnReferenceRadius / groundScale;
        foreach (string partName in _columnPartNames)
        {
            Transform part = _effect.transform.Find(partName);
            if (part == null)
                continue;

            // 커지며 나타나는 연출은 생성 때 잡아 둔 원래 크기를 쓰므로 그 값을 함께 바꾼다.
            if (part.TryGetComponent(out FxGrowIn growIn))
                growIn.MultiplyBaseScale(ratio);
            else
                part.localScale *= ratio;
        }
    }
}
