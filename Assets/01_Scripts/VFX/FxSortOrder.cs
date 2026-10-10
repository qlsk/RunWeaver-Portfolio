using UnityEngine;

/// <summary>
/// 이펙트의 정렬 레이어와 순서를 고정한다. 회오리 기둥처럼 캐릭터 사이로 솟는 이펙트에 붙인다.
/// 캐릭터(정렬 순서 0)는 프로젝트 설정의 Y축 정렬로 앞뒤가 정해지므로,
/// 같은 순서(0)에 두고 SpriteRenderer의 Sort Point를 피벗(밑동)으로 두면 캐릭터와 앞뒤가 맞는다.
/// ReactionAreaVisuals가 정렬 순서를 바닥 아래(-10)로 바꿔도 매 프레임 다시 맞춘다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class FxSortOrder : MonoBehaviour
{
    [Tooltip("0이면 캐릭터와 Y 위치로 앞뒤가 정해지고, 크게 두면 항상 캐릭터 위에 그린다. (증기 구름)")]
    [SerializeField] private int _sortingOrder;
    [SerializeField] private string _sortingLayerName = "Default";

    private SpriteRenderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        _renderer.sortingLayerName = _sortingLayerName;
        _renderer.sortingOrder = _sortingOrder;
    }
}
