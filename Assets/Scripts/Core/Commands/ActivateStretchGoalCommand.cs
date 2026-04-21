public struct ActivateStretchGoalCommand : ICommand
{
    public int Tick { get; set; }
    public ContractId ContractId;
}
