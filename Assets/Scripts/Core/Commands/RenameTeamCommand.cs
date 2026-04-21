public struct RenameTeamCommand : ICommand
{
    public int Tick { get; set; }
    public TeamId TeamId;
    public string NewName;
}
