public struct CreateTeamCommand : ICommand
{
    public int Tick { get; set; }
    public TeamType TeamType;
    public CompanyId CompanyId;
}
