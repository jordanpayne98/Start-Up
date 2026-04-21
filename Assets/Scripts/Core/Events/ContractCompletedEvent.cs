public class ContractCompletedEvent : GameEvent
{
    public ContractId ContractId;
    public int Reward;
    
    public ContractCompletedEvent(int tick, ContractId contractId, int reward) : base(tick)
    {
        ContractId = contractId;
        Reward = reward;
    }
}
