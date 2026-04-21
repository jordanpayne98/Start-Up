public class ProductPhaseCompletedEvent : GameEvent
{
    public ProductId ProductId;
    public ProductPhaseType PhaseType;
    public float Quality;

    public ProductPhaseCompletedEvent(int tick, ProductId productId, ProductPhaseType phaseType, float quality) : base(tick)
    {
        ProductId = productId;
        PhaseType = phaseType;
        Quality = quality;
    }
}
