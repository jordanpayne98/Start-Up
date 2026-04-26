public class EmployeeHiredEvent : GameEvent
{
    public EmployeeId Id;
    public string Name;
    public EmploymentType Type;
    public ContractLengthOption Length;
    public int Salary;

    public EmployeeHiredEvent(int tick, EmployeeId id, string name, EmploymentType type, ContractLengthOption length, int salary)
        : base(tick)
    {
        Id = id;
        Name = name;
        Type = type;
        Length = length;
        Salary = salary;
    }
}
