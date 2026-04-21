public class ProductCreatedEvent : GameEvent
{
    public ProductId ProductId;

    public ProductCreatedEvent(int tick, ProductId productId) : base(tick)
    {
        ProductId = productId;
    }
}
