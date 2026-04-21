public class ContractFailedEvent : GameEvent
{
    public ContractId ContractId;
    public string ContractName;
    public string Reason;
    
    public ContractFailedEvent(int tick, ContractId contractId, string contractName, string reason) : base(tick)
    {
        ContractId = contractId;
        ContractName = contractName;
        Reason = reason;
    }
}
