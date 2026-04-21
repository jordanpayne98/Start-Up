public class ProductPhaseUnlockedEvent : GameEvent
{
    public ProductId ProductId;
    public ProductPhaseType PhaseType;

    public ProductPhaseUnlockedEvent(int tick, ProductId productId, ProductPhaseType phaseType) : base(tick)
    {
        ProductId = productId;
        PhaseType = phaseType;
    }
}
