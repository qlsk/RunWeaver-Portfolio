using UnityEngine;

/// <summary>
/// 룬 효과 아이콘 한 개. DB의 룬 효과 id(예: rune_fire)와 스프라이트를 짝지어 둔다.
/// </summary>
[System.Serializable]
public class RuneEffectIconData
{
    [SerializeField] private string _id;
    [SerializeField] private Sprite _icon;

    public string Id => _id;
    public Sprite Icon => _icon;
}
