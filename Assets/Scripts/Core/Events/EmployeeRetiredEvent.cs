public class EmployeeRetiredEvent : GameEvent
{
    public EmployeeId EmployeeId { get; }

    public EmployeeRetiredEvent(int tick, EmployeeId id)
        : base(tick)
    {
        EmployeeId = id;
    }
}
