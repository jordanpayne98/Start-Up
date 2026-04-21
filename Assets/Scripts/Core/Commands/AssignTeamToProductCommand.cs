public struct AssignTeamToProductCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public TeamId TeamId;
    public ProductTeamRole RoleSlot;
}
