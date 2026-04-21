public class ProductDeadEvent : GameEvent
{
    public ProductId ProductId;

    public ProductDeadEvent(int tick, ProductId productId) : base(tick)
    {
        ProductId = productId;
    }
}
