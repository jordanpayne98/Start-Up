public class ProductPhaseIterationStartedEvent : GameEvent
{
    public ProductId ProductId;
    public ProductPhaseType PhaseType;
    public int IterationCount;

    public ProductPhaseIterationStartedEvent(int tick, ProductId productId, ProductPhaseType phaseType, int iterationCount) : base(tick)
    {
        ProductId = productId;
        PhaseType = phaseType;
        IterationCount = iterationCount;
    }
}
