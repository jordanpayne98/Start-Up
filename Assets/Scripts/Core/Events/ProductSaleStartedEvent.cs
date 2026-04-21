public class ProductSaleStartedEvent : GameEvent
{
    public ProductId ProductId;

    public ProductSaleStartedEvent(int tick, ProductId productId) : base(tick)
    {
        ProductId = productId;
    }
}
