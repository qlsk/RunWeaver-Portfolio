using UnityEngine;

/// <summary>
/// 결과창에 표시할 자원 한 종류와 획득량
/// </summary>
public sealed class ResourceAmountData
{
    public string Id { get; }
    public Sprite Icon { get; }
    public int Amount { get; }

    public ResourceAmountData(string id, Sprite icon, int amount)
    {
        Id = id;
        Icon = icon;
        Amount = amount;
    }
}
