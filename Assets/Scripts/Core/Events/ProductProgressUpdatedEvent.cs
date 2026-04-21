public class ProductProgressUpdatedEvent : GameEvent
{
    public ProductId ProductId;

    public ProductProgressUpdatedEvent(int tick, ProductId productId) : base(tick)
    {
        ProductId = productId;
    }
}
