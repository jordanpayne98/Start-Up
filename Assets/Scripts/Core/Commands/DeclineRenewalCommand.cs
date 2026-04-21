public struct DeclineRenewalCommand : ICommand
{
    public int Tick { get; set; }
    public EmployeeId EmployeeId;
}
