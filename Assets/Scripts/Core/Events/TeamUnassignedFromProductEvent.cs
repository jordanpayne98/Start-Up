public class TeamUnassignedFromProductEvent : GameEvent
{
    public ProductId ProductId;
    public TeamId TeamId;

    public TeamUnassignedFromProductEvent(int tick, ProductId productId, TeamId teamId) : base(tick)
    {
        ProductId = productId;
        TeamId = teamId;
    }
}
