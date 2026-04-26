public class EmployeeCooldownExpiredEvent : GameEvent
{
    public EmployeeId EmployeeId;
    public string EmployeeName;

    public EmployeeCooldownExpiredEvent(int tick, EmployeeId employeeId, string employeeName = "")
        : base(tick)
    {
        EmployeeId = employeeId;
        EmployeeName = employeeName;
    }
}
