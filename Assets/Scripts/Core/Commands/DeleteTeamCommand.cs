public struct DeleteTeamCommand : ICommand
{
    public int Tick { get; set; }
    public TeamId TeamId;
}
