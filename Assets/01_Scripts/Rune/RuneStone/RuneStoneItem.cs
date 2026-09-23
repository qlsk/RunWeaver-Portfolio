public class RuneStoneItem
{
    private RuneStone _runeStone;
    private int _quantity;

    public RuneStone RuneStone => _runeStone;
    public int Quantity => _quantity;

    public RuneStoneItem(RuneStone runeStone, int quantity)
    {
        _runeStone = runeStone;
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
