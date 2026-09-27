public class RuneStoneItem
{
    private RuneStoneDefinition _runeStoneDefinition;
    private int _quantity;

    public RuneStoneDefinition RuneStoneDefinition => _runeStoneDefinition;
    public int Quantity => _quantity;

    public RuneStoneItem(RuneStoneDefinition runeStoneDefinition, int quantity)
    {
        _runeStoneDefinition = runeStoneDefinition;
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
