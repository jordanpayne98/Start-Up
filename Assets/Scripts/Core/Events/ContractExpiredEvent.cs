public class ContractExpiredEvent : GameEvent
{
    public ContractId ContractId;
    
    public ContractExpiredEvent(int tick, ContractId contractId) : base(tick)
    {
        ContractId = contractId;
    }
}
