public struct PauseContractCommand : ICommand
{
    public int Tick { get; set; }
    public ContractId ContractId;
    public string Reason;
}
