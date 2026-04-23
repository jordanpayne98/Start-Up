public class ProductIdentityChangedEvent : GameEvent
{
    public ProductId ProductId { get; }
    public ProductIdentitySnapshot Previous { get; }
    public ProductIdentitySnapshot Current { get; }

    public ProductIdentityChangedEvent(int tick, ProductId productId, ProductIdentitySnapshot previous, ProductIdentitySnapshot current) : base(tick)
    {
        ProductId = productId;
        Previous = previous;
        Current = current;
    }
}
