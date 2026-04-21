public struct UnassignTeamFromContractCommand : ICommand
{
    public int Tick { get; set; }
    public ContractId ContractId;
}
