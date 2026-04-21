public class ProductMaintenanceChangedEvent : GameEvent
{
    public ProductId ProductId;
    public bool IsMaintained;

    public ProductMaintenanceChangedEvent(int tick, ProductId productId, bool isMaintained) : base(tick)
    {
        ProductId = productId;
        IsMaintained = isMaintained;
    }
}
