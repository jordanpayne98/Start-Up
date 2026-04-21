public class ContractRenewedEvent : GameEvent
{
    public EmployeeId EmployeeId;
    public int NewSalary;
    public int OldSalary;

    public ContractRenewedEvent(int tick, EmployeeId employeeId, int newSalary, int oldSalary)
        : base(tick)
    {
        EmployeeId = employeeId;
        NewSalary = newSalary;
        OldSalary = oldSalary;
    }
}
