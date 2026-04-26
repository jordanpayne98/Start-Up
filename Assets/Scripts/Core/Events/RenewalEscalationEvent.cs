public class RenewalEscalationEvent : GameEvent
{
    public EmployeeId Id;
    public string Name;
    public int StrikeCount;
    public bool IsFinalStrike;

    public RenewalEscalationEvent(int tick, EmployeeId id, string name, int strikeCount, bool isFinalStrike)
        : base(tick)
    {
        Id = id;
        Name = name;
        StrikeCount = strikeCount;
        IsFinalStrike = isFinalStrike;
    }
}
