public class ProductShipWarningEvent : GameEvent
{
    public ProductId ProductId;
    public string ProductName;
    public int IncompletePhasesCount;
    public int DaysRemaining;

    public ProductShipWarningEvent(int tick, ProductId productId, string productName, int incompletePhasesCount, int daysRemaining) : base(tick)
    {
        ProductId = productId;
        ProductName = productName;
        IncompletePhasesCount = incompletePhasesCount;
        DaysRemaining = daysRemaining;
    }
}
