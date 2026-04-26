public struct MakeOfferCommand : ICommand
{
    public int Tick { get; set; }
    public int CandidateId;
    public int OfferedSalary;
    public HiringMode Mode;
    public EmploymentType EmploymentType;
    public ContractLengthOption Length;
    public EmployeeRole OfferedRole;
}
