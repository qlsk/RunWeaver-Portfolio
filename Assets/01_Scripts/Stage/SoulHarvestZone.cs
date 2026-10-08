using UnityEngine;

/// <summary>
/// 영혼 수집 미션을 위한 특정 수집 구역 컴포넌트.
/// 이 구역 내부에서 사망한 적에게서만 영혼이 추출됩니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SoulHarvestZone : MonoBehaviour
{
    [Header("구역 시각화 설정 (기즈모)")]
    [SerializeField] private Color _gizmoColor = new Color(0.2f, 0.8f, 1f, 0.3f);
    [SerializeField] private Color _wireColor = new Color(0.2f, 0.8f, 1f, 0.9f);

    private Collider2D _zoneCollider;

    private void Awake()
    {
        ResolveZoneCollider();
    }

    /// <summary>
    /// 자식 circle의 CircleCollider2D를 구역으로 쓴다. 없으면 자신의 콜라이더로 폴백.
    /// </summary>
    private void ResolveZoneCollider()
    {
        CircleCollider2D[] childCircles = GetComponentsInChildren<CircleCollider2D>(true);
        foreach (CircleCollider2D circle in childCircles)
        {
            if (circle != null && circle.gameObject != gameObject)
            {
                _zoneCollider = circle;
                // 구역은 판정용이라 막히면 안 된다. 자식 circle이 solid였으면 트리거로 바꾼다.
                if (!_zoneCollider.isTrigger)
                {
                    _zoneCollider.isTrigger = true;
                    Debug.Log("[SoulHarvestZone] 자식 circle 콜라이더를 트리거로 전환 (이동 차단 방지)", this);
                }

                return;
            }
        }

        _zoneCollider = GetComponent<Collider2D>();
        if (_zoneCollider != null && !_zoneCollider.isTrigger)
        {
            _zoneCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// 지정된 월드 좌표가 이 구역 내부인지 판정한다.
    /// </summary>
    public bool IsInsideZone(Vector2 worldPosition)
    {
        if (_zoneCollider == null)
        {
            ResolveZoneCollider();
        }

        if (_zoneCollider == null) return false;

        return _zoneCollider.OverlapPoint(worldPosition);
    }

    private void OnDrawGizmos()
    {
        Collider2D col = _zoneCollider != null
            ? _zoneCollider
            : GetComponentInChildren<CircleCollider2D>(true) as Collider2D;
        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }

        if (col == null) return;

        Vector3 center = col.transform.position + (Vector3)GetColliderOffset(col);
        Gizmos.color = _gizmoColor;
        if (col is CircleCollider2D circle)
        {
            Gizmos.DrawSphere(center, circle.radius);
            Gizmos.color = _wireColor;
            Gizmos.DrawWireSphere(center, circle.radius);
        }
        else if (col is BoxCollider2D box)
        {
            Gizmos.DrawCube(center, box.size);
            Gizmos.color = _wireColor;
            Gizmos.DrawWireCube(center, box.size);
        }
    }

    private static Vector2 GetColliderOffset(Collider2D col)
    {
        if (col is CircleCollider2D circle) return circle.offset;
        if (col is BoxCollider2D box) return box.offset;
        if (col is CapsuleCollider2D capsule) return capsule.offset;
        return Vector2.zero;
    }
}
