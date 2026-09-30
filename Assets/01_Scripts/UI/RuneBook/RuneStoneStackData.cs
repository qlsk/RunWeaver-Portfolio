using UnityEngine;

/// <summary>
/// 보유 룬석 목록에 표시할 룬석 한 종류의 정보
/// </summary>
public sealed class RuneStoneStackData
{
    public string Id { get; }
    public char RuneCharacter { get; }
    public Sprite Icon { get; }
    public int Quantity { get; }

    public RuneStoneStackData(string id, char runeCharacter, Sprite icon, int quantity)
    {
        Id = id;
        RuneCharacter = runeCharacter;
        Icon = icon;
        Quantity = quantity;
    }
}
