using Sirenix.OdinInspector;
using UnityEngine;

/// 원소 반응 영역의 중심에 반복 재생하는 회오리 스프라이트 하나를 생성한다.
public class ReactionAreaVisuals : MonoBehaviour
{
    [FoldoutGroup("References"), SerializeField] private GameObject _effectPrefab;
    [FoldoutGroup("Visual Settings"), SerializeField, Min(0.01f)] private float _referenceRadius = 4f;

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
        // Field(-20) 위, 일반 오브젝트(-1 이상) 아래에 고정한다.
        foreach (SpriteRenderer renderer in _effect.GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = -10;
        }
    }
}
