public class ContractAcceptedEvent : GameEvent
{
    public ContractId ContractId;
    
    public ContractAcceptedEvent(int tick, ContractId contractId) : base(tick)
    {
        ContractId = contractId;
    }
}
