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
        _zoneCollider = GetComponent<Collider2D>();
        if (!_zoneCollider.isTrigger)
        {
            _zoneCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// 지정된 월드 좌표가 이 구역 내부인지 판정합니다.
    /// </summary>
    public bool IsInsideZone(Vector2 worldPosition)
    {
        if (_zoneCollider == null)
        {
            _zoneCollider = GetComponent<Collider2D>();
        }

        if (_zoneCollider == null) return false;

        return _zoneCollider.OverlapPoint(worldPosition);
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = _gizmoColor;
        if (col is CircleCollider2D circle)
        {
            Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
            Gizmos.color = _wireColor;
            Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
        }
        else if (col is BoxCollider2D box)
        {
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
            Gizmos.color = _wireColor;
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
    }
}
