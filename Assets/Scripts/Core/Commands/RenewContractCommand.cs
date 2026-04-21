public struct RenewContractCommand : ICommand
{
    public int Tick { get; set; }
    public EmployeeId EmployeeId;
}
