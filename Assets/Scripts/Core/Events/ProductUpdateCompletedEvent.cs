public class ProductUpdateCompletedEvent : GameEvent
{
    public ProductId ProductId;
    public ProductUpdateType UpdateType;

    public ProductUpdateCompletedEvent(int tick, ProductId productId, ProductUpdateType updateType) : base(tick)
    {
        ProductId = productId;
        UpdateType = updateType;
    }
}
