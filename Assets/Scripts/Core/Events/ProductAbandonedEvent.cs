public class ProductAbandonedEvent : GameEvent
{
    public ProductId ProductId;

    public ProductAbandonedEvent(int tick, ProductId productId) : base(tick)
    {
        ProductId = productId;
    }
}
