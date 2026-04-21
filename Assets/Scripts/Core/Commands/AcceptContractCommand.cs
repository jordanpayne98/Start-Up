public struct AcceptContractCommand : ICommand
{
    public int Tick { get; set; }
    public ContractId ContractId;
}
