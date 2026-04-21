public class ProductRemovedFromMarketEvent : GameEvent
{
    public ProductId ProductId;

    public ProductRemovedFromMarketEvent(int tick, ProductId productId) : base(tick)
    {
        ProductId = productId;
    }
}
