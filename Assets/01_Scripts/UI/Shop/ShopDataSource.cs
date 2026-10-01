using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 UI가 쓰는 데이터. 진열 품목, 보유 골드, 구매와 리롤을 제공한다.
/// 단독 테스트는 CatalogShopDataSource, 실제 게임 연결은 이 클래스를 상속한 별도 소스로 교체한다.
/// </summary>
public abstract class ShopDataSource : MonoBehaviour
{
    /// <summary>진열, 골드, 리롤 비용 중 하나라도 바뀌면 발생한다.</summary>
    public event Action Changed;

    /// <summary>품목을 샀을 때 발생한다. 실제 아이템 지급은 이 이벤트를 받는 쪽에서 처리한다.</summary>
    public event Action<ShopOfferData> Purchased;

    /// <summary>칸별 진열 품목. 산 칸은 null이다.</summary>
    public abstract IReadOnlyList<ShopOfferData> Offers { get; }

    public abstract int Gold { get; }

    public abstract int RerollCost { get; }

    /// <summary>상점을 연다. 리롤 비용을 처음 값으로 되돌리고 새로 진열한다.</summary>
    public abstract void Open();

    public abstract bool TryBuy(int slotIndex);

    public abstract bool TryReroll();

    public bool CanAfford(int price)
    {
        return Gold >= price;
    }

    public bool CanBuy(int slotIndex)
    {
        ShopOfferData offer = GetOffer(slotIndex);
        return offer != null && CanAfford(offer.Price);
    }

    public bool CanReroll()
    {
        return CanAfford(RerollCost);
    }

    public ShopOfferData GetOffer(int slotIndex)
    {
        IReadOnlyList<ShopOfferData> offers = Offers;
        return offers != null && slotIndex >= 0 && slotIndex < offers.Count ? offers[slotIndex] : null;
    }

    protected void RaiseChanged()
    {
        SafeEventNotifier.Notify(Changed);
    }

    protected void RaisePurchased(ShopOfferData offer)
    {
        SafeEventNotifier.Notify(Purchased, offer);
    }
}
