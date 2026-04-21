public class ContractAssignedEvent : GameEvent
{
    public ContractId ContractId;
    public TeamId TeamId;
    
    public ContractAssignedEvent(int tick, ContractId contractId, TeamId teamId) : base(tick)
    {
        ContractId = contractId;
        TeamId = teamId;
    }
}
