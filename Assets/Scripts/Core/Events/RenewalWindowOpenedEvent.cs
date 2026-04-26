public class RenewalWindowOpenedEvent : GameEvent
{
    public EmployeeId Id;
    public string Name;
    public EmploymentType CurrentType;
    public int DaysUntilExpiry;

    public RenewalWindowOpenedEvent(int tick, EmployeeId id, string name, EmploymentType currentType, int daysUntilExpiry)
        : base(tick)
    {
        Id = id;
        Name = name;
        CurrentType = currentType;
        DaysUntilExpiry = daysUntilExpiry;
    }
}
