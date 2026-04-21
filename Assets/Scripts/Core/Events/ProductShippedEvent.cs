public class ProductShippedEvent : GameEvent
{
    public ProductId ProductId;
    public float Quality;

    public ProductShippedEvent(int tick, ProductId productId, float quality) : base(tick)
    {
        ProductId = productId;
        Quality = quality;
    }
}
