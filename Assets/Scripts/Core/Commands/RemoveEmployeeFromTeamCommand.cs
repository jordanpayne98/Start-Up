public struct RemoveEmployeeFromTeamCommand : ICommand
{
    public int Tick { get; set; }
    public EmployeeId EmployeeId;
}
