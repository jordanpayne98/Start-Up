public class EmployeeFrustratedEvent : GameEvent
{
    public EmployeeId EmployeeId;
    public string EmployeeName;
    public int CooldownExpiryTick;

    public EmployeeFrustratedEvent(int tick, EmployeeId employeeId, string employeeName, int cooldownExpiryTick)
        : base(tick)
    {
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        CooldownExpiryTick = cooldownExpiryTick;
    }
}
