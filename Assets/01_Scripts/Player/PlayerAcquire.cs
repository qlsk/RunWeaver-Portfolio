using UnityEngine;

public class PlayerAcquire : MonoBehaviour
{
    [SerializeField] private RuneStoneInventory _runeStoneInventory;

    private void Awake()
    {
        if (_runeStoneInventory == null)
        {
            _runeStoneInventory = GetComponentInParent<RuneStoneInventory>();
        }

        if (_runeStoneInventory == null)
        {
            _runeStoneInventory = FindFirstObjectByType<RuneStoneInventory>();
        }

        if (_runeStoneInventory == null)
        {
            Debug.LogError("씬에서 RuneStoneInventory를 찾지 못했습니다.", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActiveAndEnabled || _runeStoneInventory == null) return;

        RunePickup pickup = collision.GetComponentInParent<RunePickup>();
        if (pickup == null) return;

        var database = RuneStoneDatabase.Instance;
        if (database == null || !database.DatabaseReady) return;
        if (!pickup.TryBeginPickup()) return;

        if (_runeStoneInventory.TryAddRuneStone(pickup.RuneCharacter, pickup.Quantity))
        {
            pickup.CompletePickup();
        }
        else
        {
            pickup.CancelPickup();
        }
    }
}