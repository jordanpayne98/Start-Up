public class EmployeeDepartedEvent : GameEvent
{
    public EmployeeId Id;
    public string Name;
    public string Reason;

    public EmployeeDepartedEvent(int tick, EmployeeId id, string name, string reason)
        : base(tick)
    {
        Id = id;
        Name = name;
        Reason = reason;
    }
}
