public class ProductLifecycleChangedEvent : GameEvent
{
    public ProductId ProductId;
    public ProductLifecycleStage OldStage;
    public ProductLifecycleStage NewStage;

    public ProductLifecycleChangedEvent(int tick, ProductId productId, ProductLifecycleStage oldStage, ProductLifecycleStage newStage) : base(tick)
    {
        ProductId = productId;
        OldStage = oldStage;
        NewStage = newStage;
    }
}
