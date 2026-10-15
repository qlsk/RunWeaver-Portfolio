using UnityEngine;

/// <summary>
/// 플레이어가 룬석(RunePickup)에 닿으면 주워서 PlayerRuneManager의 룬 문자 인벤토리에 넣는다.
/// </summary>
public class PlayerAcquire : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActiveAndEnabled) return;

        RunePickup pickup = collision.GetComponentInParent<RunePickup>();
        if (pickup == null) return;

        PlayerRuneManager runeManager = PlayerRuneManager.Instance;
        if (runeManager == null || !runeManager.IsInitialized) return;

        var database = RuneStoneDatabase.Instance;
        if (database == null || !database.DatabaseReady) return;
        if (!pickup.TryBeginPickup()) return;

        if (runeManager.RuneStoneInventory.TryAddRuneStone(pickup.RuneCharacter, pickup.Quantity))
        {
            pickup.CompletePickup();
        }
        else
        {
            pickup.CancelPickup();
        }
    }
}
