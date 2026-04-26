public struct RenewContractCommand : ICommand
{
    public int Tick { get; set; }
    public EmployeeId EmployeeId;
    public EmploymentType NewType;
    public ContractLengthOption NewLength;
    public bool AcceptsRequest;
}
