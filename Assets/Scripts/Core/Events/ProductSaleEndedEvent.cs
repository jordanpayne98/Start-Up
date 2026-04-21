public class ProductSaleEndedEvent : GameEvent
{
    public ProductId ProductId;

    public ProductSaleEndedEvent(int tick, ProductId productId) : base(tick)
    {
        ProductId = productId;
    }
}
