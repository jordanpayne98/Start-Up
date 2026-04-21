public class ProductMonthlyRevenueEvent : GameEvent
{
    public ProductId ProductId;
    public int Amount;

    public ProductMonthlyRevenueEvent(int tick, ProductId productId, int amount) : base(tick)
    {
        ProductId = productId;
        Amount = amount;
    }
}
