using UnityEngine;

/// <summary>
/// 상점 칸 하나에 진열된 품목. 가격은 진열될 때마다 정해진다.
/// </summary>
public class ShopOfferData
{
    public ShopOfferData(string id, string displayName, string description, Sprite icon, ShopItemType type, int price,
        float healRatio = 0f)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Icon = icon;
        Type = type;
        Price = price;
        HealRatio = healRatio;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public Sprite Icon { get; }
    public ShopItemType Type { get; }
    public int Price { get; }

    /// <summary>포션 : 최대 체력 대비 회복 비율 (포션이 아니면 0)</summary>
    public float HealRatio { get; }
}
