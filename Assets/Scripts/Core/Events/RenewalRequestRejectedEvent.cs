public class RenewalRequestRejectedEvent : GameEvent
{
    public EmployeeId Id;
    public string Name;

    public RenewalRequestRejectedEvent(int tick, EmployeeId id, string name)
        : base(tick)
    {
        Id = id;
        Name = name;
    }
}
