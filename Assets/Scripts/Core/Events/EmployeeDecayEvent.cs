public class EmployeeDecayEvent : GameEvent
{
    public EmployeeId EmployeeId { get; }
    public int CALost { get; }

    public EmployeeDecayEvent(int tick, EmployeeId id, int caLost)
        : base(tick)
    {
        EmployeeId = id;
        CALost = caLost;
    }
}
