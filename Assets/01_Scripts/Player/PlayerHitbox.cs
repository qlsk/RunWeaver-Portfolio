using UnityEngine;

/// <summary>이동 충돌과 분리된 플레이어 피격 영역.</summary>
[RequireComponent(typeof(Collider2D))]
public class PlayerHitbox : MonoBehaviour
{
    public PlayerHealth Health { get; private set; }

    private void Awake()
    {
        Health = GetComponentInParent<PlayerHealth>();
        GetComponent<Collider2D>().isTrigger = true;
    }
}
