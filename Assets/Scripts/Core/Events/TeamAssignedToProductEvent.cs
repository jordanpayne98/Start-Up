public class TeamAssignedToProductEvent : GameEvent
{
    public ProductId ProductId;
    public TeamId TeamId;
    public ProductTeamRole RoleSlot;

    public TeamAssignedToProductEvent(int tick, ProductId productId, TeamId teamId, ProductTeamRole roleSlot) : base(tick)
    {
        ProductId = productId;
        TeamId = teamId;
        RoleSlot = roleSlot;
    }
}
