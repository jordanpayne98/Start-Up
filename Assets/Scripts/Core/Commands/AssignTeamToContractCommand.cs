public struct AssignTeamToContractCommand : ICommand
{
    public int Tick { get; set; }
    public ContractId ContractId;
    public TeamId TeamId;
}
