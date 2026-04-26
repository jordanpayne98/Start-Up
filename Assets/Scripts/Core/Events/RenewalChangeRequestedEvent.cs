public class RenewalChangeRequestedEvent : GameEvent
{
    public EmployeeId Id;
    public string Name;
    public bool RequestsTypeChange;
    public EmploymentType RequestedType;
    public bool RequestsLengthChange;
    public ContractLengthOption RequestedLength;

    public RenewalChangeRequestedEvent(int tick, EmployeeId id, string name, bool requestsTypeChange, EmploymentType requestedType, bool requestsLengthChange, ContractLengthOption requestedLength)
        : base(tick)
    {
        Id = id;
        Name = name;
        RequestsTypeChange = requestsTypeChange;
        RequestedType = requestedType;
        RequestsLengthChange = requestsLengthChange;
        RequestedLength = requestedLength;
    }
}
