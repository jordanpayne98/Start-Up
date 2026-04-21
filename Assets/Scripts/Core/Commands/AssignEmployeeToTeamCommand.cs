public struct AssignEmployeeToTeamCommand : ICommand
{
    public int Tick { get; set; }
    public EmployeeId EmployeeId;
    public TeamId TeamId;
}
