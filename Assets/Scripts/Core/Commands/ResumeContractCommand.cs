public struct ResumeContractCommand : ICommand
{
    public int Tick { get; set; }
    public ContractId ContractId;
}
