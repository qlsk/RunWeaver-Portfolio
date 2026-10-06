using UnityEngine;

/// TODO: 탄환의 공격 데이터와 룬 효과를 상속하지 않는 독립 Trap.
public sealed class FixTrap : MonoBehaviour
{
    private const float Lifetime = 10f;

    private void Awake()
    {
        Destroy(gameObject, Lifetime);
    }
}