public class ProductCrisisEvent : GameEvent
{
    public ProductId ProductId;
    public CrisisEventType CrisisType;
    public string ProductName;

    public ProductCrisisEvent(int tick, ProductId productId, CrisisEventType crisisType, string productName) : base(tick)
    {
        ProductId = productId;
        CrisisType = crisisType;
        ProductName = productName;
    }
}
