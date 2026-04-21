public class ProductUpdateStartedEvent : GameEvent
{
    public ProductId ProductId;
    public ProductUpdateType UpdateType;

    public ProductUpdateStartedEvent(int tick, ProductId productId, ProductUpdateType updateType) : base(tick)
    {
        ProductId = productId;
        UpdateType = updateType;
    }
}
