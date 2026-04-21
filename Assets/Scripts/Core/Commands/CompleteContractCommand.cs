public struct CompleteContractCommand : ICommand
{
    public int Tick { get; set; }
    public ContractId ContractId;
}
