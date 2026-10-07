using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 거점 점령 미션을 위한 물리적 점령 구역 컴포넌트.
/// 플레이어가 이 구역(Trigger) 내에 서 있으면 점령 게이지가 차오릅니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CaptureZone : MonoBehaviour
{
    [Header("구역 시각화 (기즈모)")]
    [SerializeField, FormerlySerializedAs("zoneColor")] private Color _zoneColor = new Color(1f, 0.85f, 0.2f, 0.35f);
    [SerializeField, FormerlySerializedAs("wireColor")] private Color _wireColor = new Color(1f, 0.85f, 0.2f, 0.95f);

    public event Action<bool> OnPlayerPresenceChanged;

    private bool _isPlayerInside = false;
    public bool IsPlayerInside => _isPlayerInside;

    private Collider2D _col;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (_col == null)
        {
            _col = gameObject.AddComponent<CircleCollider2D>();
        }
        _col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerInside = true;
            Debug.Log("[CaptureZone] 플레이어가 거점 구역에 진입했습니다.");
            OnPlayerPresenceChanged?.Invoke(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _isPlayerInside = false;
            Debug.Log("[CaptureZone] 플레이어가 거점 구역에서 벗어났습니다.");
            OnPlayerPresenceChanged?.Invoke(false);
        }
    }

    private void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;

        Gizmos.color = _zoneColor;
        if (c is CircleCollider2D circle)
        {
            Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
            Gizmos.color = _wireColor;
            Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
        }
        else if (c is BoxCollider2D box)
        {
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
            Gizmos.color = _wireColor;
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
    }
}
