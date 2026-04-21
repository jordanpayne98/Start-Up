public class ContractRenewalRequestedEvent : GameEvent
{
    public EmployeeId EmployeeId;
    public string EmployeeName;
    public int Demand;
    public int DeadlineTick;

    public ContractRenewalRequestedEvent(int tick, EmployeeId employeeId, string employeeName, int demand, int deadlineTick)
        : base(tick)
    {
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        Demand = demand;
        DeadlineTick = deadlineTick;
    }
}
