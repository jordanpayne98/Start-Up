public struct UnassignTeamFromProductCommand : ICommand
{
    public int Tick { get; set; }
    public ProductId ProductId;
    public TeamId TeamId;
}
