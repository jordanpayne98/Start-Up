public class EmployeeCountChangedEvent : GameEvent
{
    public int TotalEmployees { get; }
    public EmployeeId EmployeeId { get; }
    public bool WasHired { get; }
    
    public EmployeeCountChangedEvent(int tick, int totalEmployees, EmployeeId employeeId, bool wasHired) : base(tick)
    {
        TotalEmployees = totalEmployees;
        EmployeeId = employeeId;
        WasHired = wasHired;
    }
}
