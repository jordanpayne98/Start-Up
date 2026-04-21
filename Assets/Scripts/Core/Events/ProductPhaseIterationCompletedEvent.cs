public class ProductPhaseIterationCompletedEvent : GameEvent
{
    public ProductId ProductId;
    public ProductPhaseType PhaseType;
    public float NewQuality;

    public ProductPhaseIterationCompletedEvent(int tick, ProductId productId, ProductPhaseType phaseType, float newQuality) : base(tick)
    {
        ProductId = productId;
        PhaseType = phaseType;
        NewQuality = newQuality;
    }
}
