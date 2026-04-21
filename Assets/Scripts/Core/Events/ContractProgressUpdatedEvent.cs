public class ContractProgressUpdatedEvent : GameEvent
{
    public ContractId ContractId;
    public float ProgressPercent;
    public float WorkThisTick;
    
    public ContractProgressUpdatedEvent(int tick, ContractId contractId, float progressPercent, float workThisTick) : base(tick)
    {
        ContractId = contractId;
        ProgressPercent = progressPercent;
        WorkThisTick = workThisTick;
    }
}
