public class EmployeeTransferredEvent : GameEvent
{
    public EmployeeId EmployeeId { get; }
    public CompanyId FromCompany { get; }
    public CompanyId ToCompany { get; }

    public EmployeeTransferredEvent(int tick, EmployeeId employeeId, CompanyId fromCompany, CompanyId toCompany)
        : base(tick)
    {
        EmployeeId = employeeId;
        FromCompany = fromCompany;
        ToCompany = toCompany;
    }
}
