using System;
using UnityEngine;

public class Portal : MonoBehaviour
{
    // 플레이어가 포탈에 진입 및 이탈할 때 발행되는 정적 이벤트
    public static event Action<Portal> OnPlayerEntered;
    public static event Action<Portal> OnPlayerExited;

    [Header("시각 효과 / 기즈모 설정")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 1f, 0.4f);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("[Portal] 플레이어가 포탈 영역에 진입했습니다.");
            OnPlayerEntered?.Invoke(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("[Portal] 플레이어가 포탈 영역에서 벗어났습니다.");
            OnPlayerExited?.Invoke(this);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
