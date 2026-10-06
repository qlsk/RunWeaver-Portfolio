using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 품목 목록과 상점 규칙 값.
/// 정식 상점 DB가 생기기 전까지 사용하는 임시 데이터이다.
/// </summary>
[CreateAssetMenu(fileName = "ShopCatalog", menuName = "UI/Shop Catalog")]
public class ShopCatalogSO : ScriptableObject
{
    [Serializable]
    public class Item
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public Sprite Icon;
        public ShopItemType Type;
        [Min(1)] public int BasePrice = 100;

        [Tooltip("룬 워드 : 사면 받는 완성품의 룬 효과 id (예: rune_pierce). 룬 워드가 아니면 비운다")]
        public string RuneEffectId;

        [Tooltip("포션 : 최대 체력 대비 회복 비율 (0.3 = 최대 체력의 30% 회복). 포션이 아니면 0")]
        [Range(0f, 1f)] public float RealRatio;
    }

    [SerializeField] private List<Item> _items = new();

    [Header("진열")]
    [SerializeField, Min(1)] private int _slotCount = 3;

    [Tooltip("진열될 때마다 기본 가격에서 ±이 비율만큼 무작위로 가격이 바뀐다. (0.2 = ±20%)")]
    [SerializeField, Range(0f, 1f)] private float _priceVariance = 0.2f;

    [Tooltip("가격과 리롤 비용을 이 단위로 반올림한다.")]
    [SerializeField, Min(1)] private int _priceRoundUnit = 10;

    [Header("리롤")]
    [SerializeField, Min(0)] private int _rerollBaseCost = 500;

    [Tooltip("리롤할 때마다 비용이 이 비율만큼 오른다. (0.2 = 20%)")]
    [SerializeField, Min(0f)] private float _rerollCostGrowth = 0.2f;

    public IReadOnlyList<Item> Items => _items;
    public int SlotCount => _slotCount;
    public float PriceVariance => _priceVariance;
    public int PriceRoundUnit => _priceRoundUnit;
    public int RerollBaseCost => _rerollBaseCost;
    public float RerollCostGrowth => _rerollCostGrowth;
}
