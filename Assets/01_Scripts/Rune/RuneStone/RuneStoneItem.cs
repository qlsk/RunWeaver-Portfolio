public class RuneStoneItem
{
    private RuneStoneData _runeStoneData;
    private int _quantity;

    public RuneStoneData RuneStoneData => _runeStoneData;
    public int Quantity => _quantity;

    public RuneStoneItem(RuneStoneData runeStoneData, int quantity)
    {
        _runeStoneData = runeStoneData;
        _quantity = quantity;
    }

    public void IncreaseQuantity(int quantity)
    {
        _quantity += quantity;
    }

    public bool DecreaseQuantity(int quantity)
    {
        if (quantity > _quantity) return false;
        _quantity -= quantity;
        return true;
    }
}
