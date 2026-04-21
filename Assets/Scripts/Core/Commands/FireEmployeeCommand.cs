public struct FireEmployeeCommand : ICommand
{
    public int Tick { get; set; }
    public EmployeeId EmployeeId;
}
